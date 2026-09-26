using School_Management_System.Models;

namespace School_Management_System.Services;

public interface IFeePdfService
{
    byte[] CreateChallanPdf(School school, FeeChallan challan, decimal currentStudentOutstanding);
    byte[] CreateChallanBatchPdf(School school, IReadOnlyList<(FeeChallan Challan, decimal CurrentStudentOutstanding)> challans);
    byte[] CreateReceiptPdf(School school, FeePayment payment, decimal currentStudentOutstanding);
}
