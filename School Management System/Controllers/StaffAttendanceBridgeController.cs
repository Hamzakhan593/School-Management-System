using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Services;
using School_Management_System.ViewModels;

namespace School_Management_System.Controllers;

[ApiController]
[Route("api/staff-attendance/bridge")]
[AllowAnonymous]
public class StaffAttendanceBridgeController : ControllerBase
{
    private const string DeviceKeyHeader = "X-Device-Key";
    private readonly ApplicationDbContext _db;
    private readonly IStaffAttendanceService _attendance;

    public StaffAttendanceBridgeController(ApplicationDbContext db, IStaffAttendanceService attendance)
    {
        _db = db;
        _attendance = attendance;
    }

    [HttpPost("events")]
    public async Task<IActionResult> ReceiveEvent([FromBody] BridgeAttendanceEventRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var device = await AuthenticateDeviceAsync(request.DeviceCode, cancellationToken);
        if (device is null) return Unauthorized(new { message = "Invalid device code/key or device is inactive." });

        var result = await _attendance.ProcessBiometricEventAsync(device, request, cancellationToken);
        return Ok(new
        {
            accepted = result.Success,
            status = result.Status.ToString(),
            result.Message,
            result.EventId,
            result.AttendanceId,
            result.StaffId
        });
    }

    private async Task<BiometricDevice?> AuthenticateDeviceAsync(string? deviceCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(deviceCode)) return null;
        if (!Request.Headers.TryGetValue(DeviceKeyHeader, out var keyValues)) return null;
        var suppliedKey = keyValues.ToString();
        if (string.IsNullOrWhiteSpace(suppliedKey)) return null;

        var code = deviceCode.Trim().ToUpperInvariant();
        var device = await _db.BiometricDevices.FirstOrDefaultAsync(x => x.DeviceCode == code && x.IsActive, cancellationToken);
        return device is not null && DeviceKeyUtility.Verify(suppliedKey, device.ApiKeyHash) ? device : null;
    }
}
