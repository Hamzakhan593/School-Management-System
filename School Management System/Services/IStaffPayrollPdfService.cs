using School_Management_System.Models;

namespace School_Management_System.Services;

public interface IStaffPayrollPdfService
{
    Task<byte[]> CreatePayslipPdfAsync(School school, PayrollItem item, CancellationToken cancellationToken = default);
}
