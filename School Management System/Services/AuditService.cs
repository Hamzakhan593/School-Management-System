using Microsoft.AspNetCore.Identity;
using School_Management_System.Data;
using School_Management_System.Models;

namespace School_Management_System.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuditService(
        ApplicationDbContext db,
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }

    public async Task WriteAsync(string action, string entityType = "", string? entityId = null, string? details = null)
    {
        var http = _httpContextAccessor.HttpContext;
        ApplicationUser? user = null;

        if (http?.User?.Identity?.IsAuthenticated == true)
        {
            user = await _userManager.GetUserAsync(http.User);
        }

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = user?.Id,
            UserEmail = user?.Email,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            IpAddress = http?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = http?.Request.Headers["User-Agent"].ToString(),
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }
}
