using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Options;
using School_Management_System.ViewModels;

namespace School_Management_System.Services;

public class BackupService : IBackupService
{
    private static readonly SemaphoreSlim OperationLock = new(1, 1);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly BackupOptions _options;
    private readonly IHttpContextAccessor _http;
    private readonly ISystemSettingsService _settings;

    public BackupService(IServiceScopeFactory scopeFactory, IConfiguration configuration, IWebHostEnvironment environment,
        IOptions<BackupOptions> options, IHttpContextAccessor http, ISystemSettingsService settings)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _environment = environment;
        _options = options.Value;
        _http = http;
        _settings = settings;
    }

    public async Task<BackupDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var schoolId = await ResolveSchoolIdAsync(scope.ServiceProvider, cancellationToken);
        var runtimeSettings = await _settings.GetAsync(schoolId, cancellationToken);
        var backups = await db.BackupRecords.AsNoTracking().Where(x => x.SchoolId == schoolId)
            .OrderByDescending(x => x.StartedAtUtc).Take(100).ToListAsync(cancellationToken);
        var restores = await db.RestoreRecords.AsNoTracking().Where(x => x.SchoolId == schoolId)
            .OrderByDescending(x => x.StartedAtUtc).Take(25).ToListAsync(cancellationToken);
        var builder = ConnectionBuilder();
        return new BackupDashboardViewModel
        {
            LatestSuccessful = backups.FirstOrDefault(x => x.Status == BackupStatus.Succeeded),
            LatestVerified = backups.FirstOrDefault(x => x.Status == BackupStatus.Succeeded && x.IsVerified),
            Backups = backups,
            Restores = restores,
            NextScheduledLocal = GetNextScheduledLocal(runtimeSettings),
            ScheduledBackupsEnabled = runtimeSettings.ScheduledBackupsEnabled,
            RetentionDays = Math.Max(1, runtimeSettings.BackupRetentionDays),
            AllowInAppRestore = _options.AllowInAppRestore && runtimeSettings.AllowInAppRestore,
            StorageRootDisplay = ResolveBackupRoot(),
            DatabaseName = builder.InitialCatalog
        };
    }

    public async Task<BackupRecord> CreateBackupAsync(BackupType type, string? notes = null, CancellationToken cancellationToken = default)
    {
        await OperationLock.WaitAsync(cancellationToken);
        try
        {
            return await CreateBackupCoreAsync(type, notes, cancellationToken);
        }
        finally { OperationLock.Release(); }
    }

    private async Task<BackupRecord> CreateBackupCoreAsync(BackupType type, string? notes, CancellationToken cancellationToken)
    {
        var builder = ConnectionBuilder();
        if (string.IsNullOrWhiteSpace(builder.InitialCatalog)) throw new InvalidOperationException("The SQL Server connection string must contain a database name.");
        var root = ResolveBackupRoot();
        Directory.CreateDirectory(root);
        var safeDb = string.Concat(builder.InitialCatalog.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_'));
        var fileName = $"{safeDb}_{DateTime.Now:yyyyMMdd_HHmmss}_{type.ToString().ToLowerInvariant()}_{Guid.NewGuid():N}.bak";
        var fullPath = Path.Combine(root, fileName);

        var actor = await ResolveActorAsync(cancellationToken);
        long recordId;
        int schoolId;
        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            schoolId = await ResolveSchoolIdAsync(scope.ServiceProvider, cancellationToken);
            var runtimeSettings = await _settings.GetAsync(schoolId, cancellationToken);
            var record = new BackupRecord
            {
                SchoolId = schoolId,
                BackupType = type,
                Status = BackupStatus.InProgress,
                FileName = fileName,
                StorageReference = fullPath,
                DatabaseName = builder.InitialCatalog,
                RequestedByUserId = actor.Id,
                RequestedByEmail = actor.Email,
                Notes = notes,
                StartedAtUtc = DateTime.UtcNow,
                RetentionUntilUtc = DateTime.UtcNow.AddDays(Math.Max(1, runtimeSettings.BackupRetentionDays))
            };
            db.BackupRecords.Add(record);
            await db.SaveChangesAsync(cancellationToken);
            recordId = record.Id;
        }

        try
        {
            await ExecuteBackupCommandAsync(builder, fullPath, cancellationToken);
            var info = new FileInfo(fullPath);
            var hash = await ComputeSha256Async(fullPath, cancellationToken);
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var record = await db.BackupRecords.FirstAsync(x => x.Id == recordId, cancellationToken);
            record.Status = BackupStatus.Succeeded;
            record.SizeBytes = info.Exists ? info.Length : null;
            record.Sha256 = hash;
            record.CompletedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(scope.ServiceProvider, "Backup.Created", "BackupRecord", record.Id.ToString(), $"Type={type}; File={fileName}; Size={record.SizeBytes}", null, null);
            await CleanupExpiredAsync(db, schoolId, cancellationToken);
            return record;
        }
        catch (Exception ex)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var record = await db.BackupRecords.FirstAsync(x => x.Id == recordId, cancellationToken);
            record.Status = BackupStatus.Failed;
            record.ErrorMessage = Truncate(ex.Message, 2000);
            record.CompletedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(scope.ServiceProvider, "Backup.Failed", "BackupRecord", record.Id.ToString(), record.ErrorMessage, null, null);
            throw;
        }
    }

    public async Task<BackupRecord> VerifyAsync(long backupId, CancellationToken cancellationToken = default)
    {
        await OperationLock.WaitAsync(cancellationToken);
        try
        {
            BackupRecord snapshot;
            await using (var scope = _scopeFactory.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var schoolId = await ResolveSchoolIdAsync(scope.ServiceProvider, cancellationToken);
                snapshot = await db.BackupRecords.AsNoTracking().FirstOrDefaultAsync(x => x.Id == backupId && x.SchoolId == schoolId, cancellationToken)
                    ?? throw new InvalidOperationException("Backup record was not found.");
            }
            EnsureBackupFileSafe(snapshot);
            await ExecuteVerifyCommandAsync(ConnectionBuilder(), snapshot.StorageReference, cancellationToken);
            var currentHash = await ComputeSha256Async(snapshot.StorageReference, cancellationToken);
            if (!string.IsNullOrWhiteSpace(snapshot.Sha256) && !string.Equals(snapshot.Sha256, currentHash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Backup file hash does not match the recorded SHA-256 value.");

            await using var updateScope = _scopeFactory.CreateAsyncScope();
            var db2 = updateScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var record = await db2.BackupRecords.FirstAsync(x => x.Id == backupId, cancellationToken);
            record.IsVerified = true;
            record.VerifiedAtUtc = DateTime.UtcNow;
            record.Sha256 ??= currentHash;
            await db2.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(updateScope.ServiceProvider, "Backup.Verified", "BackupRecord", record.Id.ToString(), $"RESTORE VERIFYONLY passed for {record.FileName}", null, null);
            return record;
        }
        finally { OperationLock.Release(); }
    }

    public async Task DeleteAsync(long backupId, CancellationToken cancellationToken = default)
    {
        await OperationLock.WaitAsync(cancellationToken);
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var schoolId = await ResolveSchoolIdAsync(scope.ServiceProvider, cancellationToken);
            var record = await db.BackupRecords.FirstOrDefaultAsync(x => x.Id == backupId && x.SchoolId == schoolId, cancellationToken)
                ?? throw new InvalidOperationException("Backup record was not found.");
            if (record.BackupType == BackupType.SafetyBeforeRestore && record.RetentionUntilUtc > DateTime.UtcNow)
                throw new InvalidOperationException("A recent safety backup cannot be deleted before its retention date.");
            if (File.Exists(record.StorageReference)) File.Delete(record.StorageReference);
            record.Status = BackupStatus.Deleted;
            record.DeletedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(scope.ServiceProvider, "Backup.Deleted", "BackupRecord", record.Id.ToString(), record.FileName, null, null);
        }
        finally { OperationLock.Release(); }
    }

    public async Task RestoreAsync(long backupId, string reason, CancellationToken cancellationToken = default)
    {
        if (!_options.AllowInAppRestore) throw new InvalidOperationException("In-app restore is disabled by configuration.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 10) throw new InvalidOperationException("A clear restore reason is required.");
        await OperationLock.WaitAsync(cancellationToken);
        try
        {
            BackupRecord target;
            int schoolId;
            await using (var scope = _scopeFactory.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                schoolId = await ResolveSchoolIdAsync(scope.ServiceProvider, cancellationToken);
                var runtimeSettings = await _settings.GetAsync(schoolId, cancellationToken);
                if (!runtimeSettings.AllowInAppRestore) throw new InvalidOperationException("In-app restore is disabled in Settings.");
                target = await db.BackupRecords.AsNoTracking().FirstOrDefaultAsync(x => x.Id == backupId && x.SchoolId == schoolId && x.Status == BackupStatus.Succeeded, cancellationToken)
                    ?? throw new InvalidOperationException("Only a successful backup can be restored.");
            }
            EnsureBackupFileSafe(target);
            await ExecuteVerifyCommandAsync(ConnectionBuilder(), target.StorageReference, cancellationToken);
            var hash = await ComputeSha256Async(target.StorageReference, cancellationToken);
            if (!string.IsNullOrWhiteSpace(target.Sha256) && !string.Equals(target.Sha256, hash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Restore stopped because backup integrity verification failed.");

            var safety = await CreateBackupCoreAsync(BackupType.SafetyBeforeRestore, $"Automatic safety backup before restoring {target.FileName}", cancellationToken);
            var actor = await ResolveActorAsync(cancellationToken);
            var started = DateTime.UtcNow;
            try
            {
                await ExecuteRestoreCommandAsync(ConnectionBuilder(), target.StorageReference, cancellationToken);
                SqlConnection.ClearAllPools();
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var restoredTarget = await db.BackupRecords.FirstOrDefaultAsync(x => x.Id == target.Id, cancellationToken);
                if (restoredTarget is not null)
                {
                    restoredTarget.Status = BackupStatus.Succeeded;
                    restoredTarget.SizeBytes = target.SizeBytes;
                    restoredTarget.Sha256 = hash;
                    restoredTarget.IsVerified = true;
                    restoredTarget.VerifiedAtUtc = DateTime.UtcNow;
                    restoredTarget.CompletedAtUtc = target.CompletedAtUtc ?? DateTime.UtcNow;
                }
                else
                {
                    db.BackupRecords.Add(new BackupRecord
                    {
                        SchoolId = schoolId, BackupType = target.BackupType, Status = BackupStatus.Succeeded,
                        FileName = target.FileName, StorageReference = target.StorageReference, DatabaseName = target.DatabaseName,
                        SizeBytes = target.SizeBytes, Sha256 = hash, IsVerified = true, VerifiedAtUtc = DateTime.UtcNow,
                        RequestedByUserId = target.RequestedByUserId, RequestedByEmail = target.RequestedByEmail, Notes = target.Notes,
                        StartedAtUtc = target.StartedAtUtc, CompletedAtUtc = target.CompletedAtUtc ?? DateTime.UtcNow, RetentionUntilUtc = target.RetentionUntilUtc
                    });
                }
                if (!await db.BackupRecords.AnyAsync(x => x.FileName == safety.FileName, cancellationToken))
                {
                    db.BackupRecords.Add(new BackupRecord
                    {
                        SchoolId = schoolId, BackupType = BackupType.SafetyBeforeRestore, Status = BackupStatus.Succeeded,
                        FileName = safety.FileName, StorageReference = safety.StorageReference, DatabaseName = safety.DatabaseName,
                        SizeBytes = safety.SizeBytes, Sha256 = safety.Sha256, IsVerified = safety.IsVerified, VerifiedAtUtc = safety.VerifiedAtUtc,
                        RequestedByUserId = safety.RequestedByUserId, RequestedByEmail = safety.RequestedByEmail, Notes = safety.Notes,
                        StartedAtUtc = safety.StartedAtUtc, CompletedAtUtc = safety.CompletedAtUtc, RetentionUntilUtc = safety.RetentionUntilUtc
                    });
                }
                db.RestoreRecords.Add(new RestoreRecord
                {
                    SchoolId = schoolId,
                    BackupRecordId = target.Id,
                    BackupFileName = target.FileName,
                    BackupSha256 = hash,
                    SafetyBackupFileName = safety.FileName,
                    Reason = reason.Trim(),
                    RequestedByUserId = actor.Id,
                    RequestedByEmail = actor.Email,
                    WasSuccessful = true,
                    StartedAtUtc = started,
                    CompletedAtUtc = DateTime.UtcNow
                });
                await db.SaveChangesAsync(cancellationToken);
                await WriteAuditAsync(scope.ServiceProvider, "Database.Restored", "BackupRecord", target.Id.ToString(), $"Restored {target.FileName}; safety backup {safety.FileName}; reason: {reason}", null, null);
            }
            catch (Exception ex)
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    db.RestoreRecords.Add(new RestoreRecord
                    {
                        SchoolId = schoolId,
                        BackupRecordId = target.Id,
                        BackupFileName = target.FileName,
                        BackupSha256 = hash,
                        SafetyBackupFileName = safety.FileName,
                        Reason = reason.Trim(),
                        RequestedByUserId = actor.Id,
                        RequestedByEmail = actor.Email,
                        WasSuccessful = false,
                        StartedAtUtc = started,
                        CompletedAtUtc = DateTime.UtcNow,
                        ErrorMessage = Truncate(ex.Message, 2000)
                    });
                    await db.SaveChangesAsync(cancellationToken);
                }
                catch { }
                throw;
            }
        }
        finally { OperationLock.Release(); }
    }

    public async Task CreateScheduledBackupIfDueAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var schoolIds = await db.Schools.AsNoTracking().OrderBy(x => x.Id).Select(x => x.Id).Take(2).ToListAsync(cancellationToken);
        if (schoolIds.Count == 0) return;
        if (schoolIds.Count > 1) return; // M19 is single-school first; multi-school scheduling becomes vendor-level later.

        var runtimeSettings = await _settings.GetAsync(schoolIds[0], cancellationToken);
        if (!runtimeSettings.ScheduledBackupsEnabled) return;

        var zone = ResolveTimeZone(runtimeSettings.SchoolTimeZoneId);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
        if (localNow.Hour < Math.Clamp(runtimeSettings.BackupHourLocal, 0, 23)) return;

        var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(localNow.Date, zone);
        var already = await db.BackupRecords.AsNoTracking().AnyAsync(x => x.BackupType == BackupType.Scheduled && x.StartedAtUtc >= dayStartUtc && x.Status != BackupStatus.Failed, cancellationToken);
        if (already) return;

        await CreateBackupAsync(BackupType.Scheduled, "Automatic daily scheduled backup", cancellationToken);
    }

    private async Task ExecuteBackupCommandAsync(SqlConnectionStringBuilder builder, string fullPath, CancellationToken ct)
    {
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(ct);
        var database = EscapeIdentifier(builder.InitialCatalog);
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 0;
        command.CommandText = $"BACKUP DATABASE {database} TO DISK = @path WITH INIT, CHECKSUM, COPY_ONLY, STATS = 10;";
        command.Parameters.AddWithValue("@path", fullPath);
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task ExecuteVerifyCommandAsync(SqlConnectionStringBuilder builder, string fullPath, CancellationToken ct)
    {
        var master = new SqlConnectionStringBuilder(builder.ConnectionString) { InitialCatalog = "master" };
        await using var connection = new SqlConnection(master.ConnectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 0;
        command.CommandText = "RESTORE VERIFYONLY FROM DISK = @path WITH CHECKSUM;";
        command.Parameters.AddWithValue("@path", fullPath);
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task ExecuteRestoreCommandAsync(SqlConnectionStringBuilder builder, string fullPath, CancellationToken ct)
    {
        SqlConnection.ClearAllPools();
        var database = EscapeIdentifier(builder.InitialCatalog);
        var master = new SqlConnectionStringBuilder(builder.ConnectionString) { InitialCatalog = "master" };
        await using var connection = new SqlConnection(master.ConnectionString);
        await connection.OpenAsync(ct);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandTimeout = 0;
            command.CommandText = $"ALTER DATABASE {database} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE {database} FROM DISK = @path WITH REPLACE, CHECKSUM; ALTER DATABASE {database} SET MULTI_USER;";
            command.Parameters.AddWithValue("@path", fullPath);
            await command.ExecuteNonQueryAsync(ct);
        }
        catch
        {
            try
            {
                await using var recovery = connection.CreateCommand();
                recovery.CommandTimeout = 60;
                recovery.CommandText = $"IF DB_ID(@db) IS NOT NULL ALTER DATABASE {database} SET MULTI_USER;";
                recovery.Parameters.AddWithValue("@db", builder.InitialCatalog);
                await recovery.ExecuteNonQueryAsync(CancellationToken.None);
            }
            catch { }
            throw;
        }
    }

    private async Task CleanupExpiredAsync(ApplicationDbContext db, int schoolId, CancellationToken ct)
    {
        var expired = await db.BackupRecords.Where(x => x.SchoolId == schoolId && x.Status == BackupStatus.Succeeded && x.RetentionUntilUtc < DateTime.UtcNow).ToListAsync(ct);
        foreach (var record in expired)
        {
            try { if (File.Exists(record.StorageReference)) File.Delete(record.StorageReference); }
            catch { continue; }
            record.Status = BackupStatus.Deleted;
            record.DeletedAtUtc = DateTime.UtcNow;
        }
        if (expired.Count > 0) await db.SaveChangesAsync(ct);
    }

    private void EnsureBackupFileSafe(BackupRecord record)
    {
        if (record.Status is BackupStatus.Deleted or BackupStatus.Failed) throw new InvalidOperationException("This backup is not available.");
        var root = Path.GetFullPath(ResolveBackupRoot()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(record.StorageReference);
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Backup path is outside the configured backup directory.");
        if (!File.Exists(path)) throw new FileNotFoundException("The backup file no longer exists on disk.", record.FileName);
    }

    private SqlConnectionStringBuilder ConnectionBuilder()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection was not found.");
        return new SqlConnectionStringBuilder(connectionString);
    }

    private string ResolveBackupRoot()
    {
        var configured = string.IsNullOrWhiteSpace(_options.RootPath) ? "App_Data/Backups" : _options.RootPath.Trim();
        return Path.GetFullPath(Path.IsPathRooted(configured) ? configured : Path.Combine(_environment.ContentRootPath, configured));
    }

    private DateTime? GetNextScheduledLocal(SystemSetting settings)
    {
        if (!settings.ScheduledBackupsEnabled) return null;
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ResolveTimeZone(settings.SchoolTimeZoneId));
        var next = local.Date.AddHours(Math.Clamp(settings.BackupHourLocal, 0, 23));
        if (next <= local) next = next.AddDays(1);
        return next;
    }

    private TimeZoneInfo ResolveTimeZone(string? configuredId = null)
    {
        foreach (var id in new[] { configuredId, _options.TimeZoneId, "Asia/Karachi", "Pakistan Standard Time", "UTC" }.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id!); } catch { }
        }
        return TimeZoneInfo.Utc;
    }

    private async Task<int> ResolveSchoolIdAsync(IServiceProvider provider, CancellationToken ct)
    {
        var db = provider.GetRequiredService<ApplicationDbContext>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var principal = _http.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated == true)
        {
            var user = await userManager.GetUserAsync(principal);
            if (user?.SchoolId is int schoolId) return schoolId;
        }
        var schoolIds = await db.Schools.AsNoTracking().OrderBy(x => x.Id).Select(x => x.Id).Take(2).ToListAsync(ct);
        if (schoolIds.Count == 0) throw new InvalidOperationException("Create the school profile before using backup management.");
        if (schoolIds.Count > 1) throw new InvalidOperationException("A school context is required before backing up a multi-school database.");
        return schoolIds[0];
    }

    private async Task<(string? Id, string? Email)> ResolveActorAsync(CancellationToken ct)
    {
        if (_http.HttpContext?.User?.Identity?.IsAuthenticated != true) return (null, "SYSTEM");
        await using var scope = _scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.GetUserAsync(_http.HttpContext.User);
        return (user?.Id, user?.Email);
    }

    private static async Task WriteAuditAsync(IServiceProvider provider, string action, string entityType, string? entityId, string? details, string? oldValues, string? newValues)
    {
        var audit = provider.GetRequiredService<IAuditService>();
        await audit.WriteAsync(action, entityType, entityId, details, oldValues, newValues);
    }

    private static string EscapeIdentifier(string value) => $"[{value.Replace("]", "]]", StringComparison.Ordinal)}]";
    private static string Truncate(string? value, int max) => string.IsNullOrEmpty(value) || value.Length <= max ? value ?? string.Empty : value[..max];

    private static async Task<string> ComputeSha256Async(string file, CancellationToken ct)
    {
        await using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[1024 * 1024];
        int read;
        while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0) hash.AppendData(buffer, 0, read);
        return Convert.ToHexString(hash.GetHashAndReset());
    }
}
