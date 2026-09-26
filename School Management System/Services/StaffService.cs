using System.Data;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;

namespace School_Management_System.Services;

public class StaffService : IStaffService
{
    private readonly ApplicationDbContext _db;

    public StaffService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(bool Success, string Message, int? StaffId, string? EmployeeId)> CreateAsync(
        Staff staff,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var year = staff.JoiningDate.Year;
        var counter = await _db.StaffNumberCounters
            .FirstOrDefaultAsync(x => x.SchoolId == staff.SchoolId && x.Year == year, cancellationToken);

        if (counter is null)
        {
            counter = new StaffNumberCounter
            {
                SchoolId = staff.SchoolId,
                Year = year,
                LastNumber = 0
            };
            _db.StaffNumberCounters.Add(counter);
        }

        counter.LastNumber++;
        staff.EmployeeId = $"EMP-{year}-{counter.LastNumber:0000}";
        staff.CreatedAtUtc = DateTime.UtcNow;

        _db.Staff.Add(staff);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return (true, $"Staff member created with employee ID {staff.EmployeeId}.", staff.Id, staff.EmployeeId);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, "A staff member with the same CNIC or employee identifier already exists.", null, null);
        }
    }
}
