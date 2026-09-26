using System.Text;
using School_Management_System.Models;

namespace School_Management_System.Services;

public class StaffPayrollPdfService : IStaffPayrollPdfService
{
    private readonly ISystemSettingsService _settings;

    public StaffPayrollPdfService(ISystemSettingsService settings)
    {
        _settings = settings;
    }

    public async Task<byte[]> CreatePayslipPdfAsync(School school, PayrollItem item, CancellationToken cancellationToken = default)
    {
        var settings = await _settings.GetAsync(school.Id, cancellationToken);
        var run = item.PayrollRun;
        var lines = new List<string>
        {
            school.Name,
            "PAYSLIP",
            $"Payroll Period: {new DateTime(run.PeriodYear, run.PeriodMonth, 1):MMMM yyyy}",
            $"Employee: {item.StaffNameSnapshot}    Employee ID: {item.EmployeeIdSnapshot}",
            $"Department: {item.DepartmentSnapshot ?? "-"}",
            "",
            "EARNINGS",
            $"Basic Salary: {Money(item.BasicSalary)}",
            $"Fixed Allowances: {Money(item.FixedAllowances)}",
            $"Manual Allowance: {Money(item.ManualAllowance)}",
            $"Gross Pay: {Money(item.GrossPay)}",
            "",
            "ATTENDANCE",
            $"Present: {item.PresentDays}    Absent: {item.AbsentDays}    Late: {item.LateDays}    Leave: {item.LeaveDays}    Half Day: {item.HalfDays}",
            "",
            "DEDUCTIONS",
            $"Attendance Deduction: {Money(item.AttendanceDeduction)}",
            $"Fixed Deduction: {Money(item.FixedDeduction)}",
            $"Advance / Loan Deduction: {Money(item.AdvanceDeduction)}",
            $"Manual Deduction: {Money(item.ManualDeduction)}",
            $"Total Deductions: {Money(item.TotalDeductions)}",
            "",
            $"NET PAY: {Money(item.NetPay)}",
            $"Payment Method: {run.PaymentMethod?.ToString() ?? "-"}",
            $"Payment Reference: {run.PaymentReference ?? "-"}",
            $"Posted: {(run.PostedAtUtc.HasValue ? run.PostedAtUtc.Value.ToLocalTime().ToString("dd MMM yyyy HH:mm") : "-")}",
            ""
        };

        if (!string.IsNullOrWhiteSpace(item.ManualAdjustmentNote))
            lines.Add("Adjustment Note: " + item.ManualAdjustmentNote);
        if (!string.IsNullOrWhiteSpace(settings.PayslipFooterText))
            lines.Add(settings.PayslipFooterText!);
        lines.Add("Employee Signature: ____________________");
        lines.Add("Authorized Signature: ____________________");
        return SimplePdfWriter.Create(lines);
    }

    private static string Money(decimal value) => $"PKR {value:N2}";

    private static class SimplePdfWriter
    {
        private static byte[] Ascii(string value) => Encoding.ASCII.GetBytes(value);

        public static byte[] Create(IReadOnlyList<string> lines)
        {
            var content = BuildContent(lines);
            var objects = new Dictionary<int, byte[]>
            {
                [1] = Ascii("<< /Type /Catalog /Pages 2 0 R >>"),
                [2] = Ascii("<< /Type /Pages /Count 1 /Kids [3 0 R] >>"),
                [3] = Ascii("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>"),
                [4] = Combine(Ascii($"<< /Length {content.Length} >>\nstream\n"), content, Ascii("\nendstream")),
                [5] = Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>")
            };

            using var ms = new MemoryStream();
            Write(ms, Ascii("%PDF-1.4\n"));
            var offsets = new long[6];
            for (var id = 1; id <= 5; id++)
            {
                offsets[id] = ms.Position;
                Write(ms, Ascii($"{id} 0 obj\n"));
                Write(ms, objects[id]);
                Write(ms, Ascii("\nendobj\n"));
            }
            var xrefOffset = ms.Position;
            Write(ms, Ascii("xref\n0 6\n0000000000 65535 f \n"));
            for (var id = 1; id <= 5; id++) Write(ms, Ascii($"{offsets[id]:0000000000} 00000 n \n"));
            Write(ms, Ascii($"trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF"));
            return ms.ToArray();
        }

        private static byte[] BuildContent(IReadOnlyList<string> lines)
        {
            var sb = new StringBuilder();
            sb.AppendLine("BT\n/F1 10 Tf\n50 800 Td");
            var first = true;
            foreach (var raw in lines.Take(58))
            {
                if (!first) sb.AppendLine("0 -14 Td");
                first = false;
                sb.Append('(').Append(Escape(ToAscii(raw))).AppendLine(") Tj");
            }
            sb.AppendLine("ET");
            return Ascii(sb.ToString());
        }

        private static string ToAscii(string value)
        {
            var normalized = value.Normalize(NormalizationForm.FormD);
            return new string(normalized.Where(c => c <= 127).ToArray()).Replace('\t', ' ');
        }
        private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        private static byte[] Combine(params byte[][] arrays)
        {
            var result = new byte[arrays.Sum(x => x.Length)];
            var offset = 0;
            foreach (var array in arrays)
            {
                Buffer.BlockCopy(array, 0, result, offset, array.Length);
                offset += array.Length;
            }
            return result;
        }
        private static void Write(Stream stream, byte[] bytes) => stream.Write(bytes, 0, bytes.Length);
    }
}
