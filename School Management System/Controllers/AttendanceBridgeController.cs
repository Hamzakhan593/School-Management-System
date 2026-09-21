using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Services;
using School_Management_System.Models;
using School_Management_System.ViewModels;

namespace School_Management_System.Controllers;

[ApiController]
[Route("api/attendance/bridge")]
[AllowAnonymous]
public class AttendanceBridgeController : ControllerBase
{
    private const string DeviceKeyHeader = "X-Device-Key";
    private readonly ApplicationDbContext _db;
    private readonly IAttendanceIntegrationService _integration;

    public AttendanceBridgeController(ApplicationDbContext db, IAttendanceIntegrationService integration)
    {
        _db = db;
        _integration = integration;
    }

    [HttpPost("events")]
    public async Task<IActionResult> ReceiveEvent([FromBody] BridgeAttendanceEventRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var device = await AuthenticateDeviceAsync(request.DeviceCode, cancellationToken);
        if (device is null)
            return Unauthorized(new { message = "Invalid device code/key or device is inactive." });

        var result = await _integration.ProcessBiometricEventAsync(device, request, cancellationToken);
        return Ok(new
        {
            accepted = result.Success,
            status = result.Status.ToString(),
            result.Message,
            result.EventId,
            result.AttendanceId,
            result.StudentId
        });
    }

    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat([FromBody] BridgeHeartbeatRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var device = await AuthenticateDeviceAsync(request.DeviceCode, cancellationToken);
        if (device is null)
            return Unauthorized(new { message = "Invalid device code/key or device is inactive." });

        device.LastSyncAtUtc = DateTime.UtcNow;
        device.LastStatusMessage = string.IsNullOrWhiteSpace(request.StatusMessage)
            ? "Connected"
            : request.StatusMessage.Trim();
        device.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { ok = true, serverTimeUtc = DateTime.UtcNow });
    }

    private async Task<BiometricDevice?> AuthenticateDeviceAsync(string? deviceCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(deviceCode)) return null;
        if (!Request.Headers.TryGetValue(DeviceKeyHeader, out var keyValues)) return null;

        var suppliedKey = keyValues.ToString();
        if (string.IsNullOrWhiteSpace(suppliedKey)) return null;

        var normalizedCode = deviceCode.Trim().ToUpperInvariant();
        var device = await _db.BiometricDevices
            .FirstOrDefaultAsync(x => x.DeviceCode == normalizedCode && x.IsActive, cancellationToken);

        return device is not null && DeviceKeyUtility.Verify(suppliedKey, device.ApiKeyHash)
            ? device
            : null;
    }
}
