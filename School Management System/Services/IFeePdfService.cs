using School_Management_System.Models;

namespace School_Management_System.Services;

public interface IFeePdfService
{
    byte[] CreateChallanPdf(School school, ChallanPrintModel challan);
    byte[] CreateChallanBatchPdf(School school, IReadOnlyList<ChallanPrintModel> challans);
    byte[] CreateReceiptPdf(School school, FeePayment payment, decimal currentStudentOutstanding);
}
