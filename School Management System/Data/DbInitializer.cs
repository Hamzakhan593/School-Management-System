using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Models;

namespace School_Management_System.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            if (!await db.Database.CanConnectAsync())
            {
                logger.LogWarning("Database is not available yet. Run the M01 EF Core migration and restart the app.");
                return;
            }

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            foreach (var role in AppRoles.All)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(role));
                    if (!result.Succeeded)
                    {
                        logger.LogError("Unable to create role {Role}: {Errors}", role,
                            string.Join("; ", result.Errors.Select(e => e.Description)));
                    }
                }
            }

            if (!configuration.GetValue<bool>("SeedAdmin:Enabled"))
            {
                return;
            }

            var email = configuration["SeedAdmin:Email"];
            var password = configuration["SeedAdmin:Password"];
            var fullName = configuration["SeedAdmin:FullName"] ?? "School Principal";

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning("SeedAdmin is enabled but Email/Password are missing.");
                return;
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = fullName,
                    IsActive = true
                };

                var create = await userManager.CreateAsync(user, password);
                if (!create.Succeeded)
                {
                    logger.LogError("Unable to create seeded principal: {Errors}",
                        string.Join("; ", create.Errors.Select(e => e.Description)));
                    return;
                }
            }

            if (!await userManager.IsInRoleAsync(user, AppRoles.Principal))
            {
                await userManager.AddToRoleAsync(user, AppRoles.Principal);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "M01 database tables are not ready. Run Add-Migration M01_IdentityUsersRoles and Update-Database, then restart the application.");
        }
    }
}
