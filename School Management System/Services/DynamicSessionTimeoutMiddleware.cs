using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using School_Management_System.Models;

namespace School_Management_System.Services;

public class DynamicSessionTimeoutMiddleware
{
    private readonly RequestDelegate _next;

    public DynamicSessionTimeoutMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ISystemSettingsService settings,
        IMemoryCache cache)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var user = await userManager.GetUserAsync(context.User);
            if (user?.SchoolId is int schoolId)
            {
                var policy = await settings.GetAsync(schoolId, context.RequestAborted);
                var timeout = TimeSpan.FromMinutes(Math.Clamp(policy.SessionTimeoutMinutes, 5, 720));
                var cacheKey = $"sms:last-activity:{user.Id}";
                var now = DateTimeOffset.UtcNow;

                if (cache.TryGetValue<DateTimeOffset>(cacheKey, out var lastActivity) && now - lastActivity > timeout)
                {
                    cache.Remove(cacheKey);
                    await signInManager.SignOutAsync();

                    if (HttpMethods.IsGet(context.Request.Method) && !context.Request.Path.StartsWithSegments("/Account"))
                    {
                        var returnUrl = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
                        context.Response.Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}&expired=1");
                        return;
                    }

                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                cache.Set(cacheKey, now, TimeSpan.FromHours(13));
            }
        }

        await _next(context);
    }
}
