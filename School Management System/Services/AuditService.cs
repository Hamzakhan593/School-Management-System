using Microsoft.AspNetCore.Identity;
using School_Management_System.Data;
using School_Management_System.Models;

namespace School_Management_System.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuditService(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }

    public async Task WriteAsync(string action, string entityType = "", string? entityId = null, string? details = null,
        string? oldValues = null, string? newValues = null)
    {
        var http = _httpContextAccessor.HttpContext;
        ApplicationUser? user = null;
        if (http?.User?.Identity?.IsAuthenticated == true) user = await _userManager.GetUserAsync(http.User);

        _db.AuditLogs.Add(new AuditLog
        {
            SchoolId = user?.SchoolId,
            UserId = user?.Id,
            UserEmail = user?.Email,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = Truncate(details, 1000),
            OldValues = Truncate(oldValues, 4000),
            NewValues = Truncate(newValues, 4000),
            IpAddress = http?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Truncate(http?.Request.Headers["User-Agent"].ToString(), 500),
            CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    private static string? Truncate(string? value, int max) => string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];
}
