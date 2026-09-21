using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;

namespace School_Management_System.Services;

public class SchoolContextService : ISchoolContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public SchoolContextService(
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
        _db = db;
    }

    public async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return await _userManager.GetUserAsync(principal);
    }

    public async Task<int?> GetCurrentSchoolIdAsync()
    {
        var user = await GetCurrentUserAsync();
        return user?.SchoolId;
    }

    public async Task<School?> GetCurrentSchoolAsync()
    {
        var schoolId = await GetCurrentSchoolIdAsync();
        if (schoolId.HasValue)
        {
            return await _db.Schools.AsNoTracking().FirstOrDefaultAsync(x => x.Id == schoolId.Value);
        }

        // The first release is single-school. Before the initial principal is linked,
        // allow an existing single school record to be discovered.
        if (await _db.Schools.CountAsync() == 1)
        {
            return await _db.Schools.AsNoTracking().FirstAsync();
        }

        return null;
    }
}
