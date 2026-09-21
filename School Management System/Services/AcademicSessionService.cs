using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;

namespace School_Management_System.Services;

public class AcademicSessionService : IAcademicSessionService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;

    public AcademicSessionService(ApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<(bool Success, string Message)> ActivateAsync(int sessionId, int schoolId)
    {
        var session = await _db.AcademicSessions.FirstOrDefaultAsync(x => x.Id == sessionId && x.SchoolId == schoolId);
        if (session is null) return (false, "Academic session not found.");
        if (session.Status != AcademicSessionStatus.Draft) return (false, "Only a Draft session can be activated.");

        var anotherActive = await _db.AcademicSessions.AnyAsync(x => x.SchoolId == schoolId && x.Status == AcademicSessionStatus.Active && x.Id != sessionId);
        if (anotherActive) return (false, "Another academic session is already active. Close it before activating this session.");

        session.Status = AcademicSessionStatus.Active;
        session.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("AcademicSession.Activated", "AcademicSession", session.Id.ToString(), session.Name);
        return (true, "Academic session activated.");
    }

    public async Task<(bool Success, string Message)> CloseAsync(int sessionId, int schoolId)
    {
        var session = await _db.AcademicSessions.FirstOrDefaultAsync(x => x.Id == sessionId && x.SchoolId == schoolId);
        if (session is null) return (false, "Academic session not found.");
        if (session.Status != AcademicSessionStatus.Active) return (false, "Only the Active session can be closed.");

        session.Status = AcademicSessionStatus.Closed;
        session.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("AcademicSession.Closed", "AcademicSession", session.Id.ToString(), session.Name);
        return (true, "Academic session closed. Historical data is now read-only for normal administrators.");
    }

    public async Task<(bool Success, string Message)> ArchiveAsync(int sessionId, int schoolId)
    {
        var session = await _db.AcademicSessions.FirstOrDefaultAsync(x => x.Id == sessionId && x.SchoolId == schoolId);
        if (session is null) return (false, "Academic session not found.");
        if (session.Status != AcademicSessionStatus.Closed) return (false, "Only a Closed session can be archived.");

        session.Status = AcademicSessionStatus.Archived;
        session.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("AcademicSession.Archived", "AcademicSession", session.Id.ToString(), session.Name);
        return (true, "Academic session archived.");
    }
}
