using System.Globalization;
using System.Text;
using School_Management_System.Models;

namespace School_Management_System.Services;

public class FeePdfService : IFeePdfService
{
    private readonly ReferenceChallanPdf _challans;
    public FeePdfService(ReferenceChallanPdf challans) => _challans = challans;
    public byte[] CreateChallanPdf(School school, ChallanPrintModel challan)
        => _challans.Create(school, new[] { challan });
    public byte[] CreateChallanBatchPdf(School school, IReadOnlyList<ChallanPrintModel> challans)
        => _challans.Create(school, challans);

    public byte[] CreateReceiptPdf(School school, FeePayment payment, decimal currentStudentOutstanding)
        => SimplePdfWriter.Create(new[] { BuildReceiptPage(school, payment, currentStudentOutstanding) });

    private static IReadOnlyList<string> BuildReceiptPage(School school, FeePayment payment, decimal currentStudentOutstanding)
    {
        var lines = new List<string>
        {
            school.Name,
            "FEE RECEIPT",
            $"Receipt No: {payment.ReceiptNumber}",
            $"Date: {payment.PaymentDateUtc.ToLocalTime():dd MMM yyyy HH:mm}",
            $"Student: {payment.Student.FullName}",
            $"Admission No: {payment.Student.AdmissionNumber}",
            $"Amount Received: {Money(payment.Amount)}",
            $"Method: {payment.PaymentMethod}",
            $"Reference: {payment.ReferenceNumber ?? "-"}",
            "",
            "Allocated Challans:",
            "--------------------------------------------------------------------------"
        };

        foreach (var group in payment.Allocations
                     .GroupBy(x => new { x.FeeChallanId, x.FeeChallan.ChallanNumber, x.FeeChallan.BillingPeriod, x.FeeChallan.DueDate })
                     .OrderBy(x => x.Key.DueDate))
            lines.Add($"{group.Key.ChallanNumber,-24} {group.Key.BillingPeriod,-10} {Money(group.Sum(x => x.Amount)),14}");

        lines.Add("--------------------------------------------------------------------------");
        lines.Add($"Current Student Outstanding: {Money(currentStudentOutstanding)}");
        if (payment.IsReversed)
            lines.Add($"REVERSED: {payment.ReversalReason ?? "No reason recorded"}");
        if (!string.IsNullOrWhiteSpace(payment.Notes))
            lines.AddRange(Wrap("Notes: " + payment.Notes, 88));
        lines.Add("");
        lines.AddRange(Wrap(school.ReceiptFooterText ?? "Payment received with thanks.", 88));
        return lines;
    }

    private static string Money(decimal value) => $"PKR {value:N2}";

    private static string Fit(string value, int length)
        => value.Length <= length ? value : value[..Math.Max(0, length - 3)] + "...";

    private static IEnumerable<string> Wrap(string text, int width)
    {
        var remaining = text.Trim();
        while (remaining.Length > width)
        {
            var cut = remaining.LastIndexOf(' ', width);
            if (cut <= 0) cut = width;
            yield return remaining[..cut].Trim();
            remaining = remaining[cut..].Trim();
        }
        if (remaining.Length > 0) yield return remaining;
    }

    private static class SimplePdfWriter
    {
        private const double PageWidth = 595;
        private const double PageHeight = 842;

        public static byte[] Create(IReadOnlyCollection<IReadOnlyList<string>> pages)
        {
            var pageList = pages.Count == 0 ? new List<IReadOnlyList<string>> { new[] { "No records." } } : pages.ToList();
            var pageCount = pageList.Count;
            var fontId = 3 + (pageCount * 2);
            var objectCount = fontId;

            var objects = new Dictionary<int, byte[]>();
            objects[1] = Ascii($"<< /Type /Catalog /Pages 2 0 R >>");

            var kidRefs = string.Join(' ', Enumerable.Range(0, pageCount).Select(i => $"{3 + (i * 2)} 0 R"));
            objects[2] = Ascii($"<< /Type /Pages /Count {pageCount} /Kids [{kidRefs}] >>");

            for (var i = 0; i < pageCount; i++)
            {
                var pageId = 3 + (i * 2);
                var contentId = pageId + 1;
                var content = BuildContent(pageList[i]);
                objects[pageId] = Ascii($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth:0} {PageHeight:0}] /Resources << /Font << /F1 {fontId} 0 R >> >> /Contents {contentId} 0 R >>");
                var streamPrefix = Ascii($"<< /Length {content.Length} >>\nstream\n");
                var streamSuffix = Ascii("\nendstream");
                objects[contentId] = Combine(streamPrefix, content, streamSuffix);
            }

            objects[fontId] = Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

            using var ms = new MemoryStream();
            Write(ms, Ascii("%PDF-1.4\n"));
            var offsets = new long[objectCount + 1];

            for (var id = 1; id <= objectCount; id++)
            {
                offsets[id] = ms.Position;
                Write(ms, Ascii($"{id} 0 obj\n"));
                Write(ms, objects[id]);
                Write(ms, Ascii("\nendobj\n"));
            }

            var xrefOffset = ms.Position;
            Write(ms, Ascii($"xref\n0 {objectCount + 1}\n"));
            Write(ms, Ascii("0000000000 65535 f \n"));
            for (var id = 1; id <= objectCount; id++)
                Write(ms, Ascii($"{offsets[id]:0000000000} 00000 n \n"));

            Write(ms, Ascii($"trailer\n<< /Size {objectCount + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF"));
            return ms.ToArray();
        }

        private static byte[] BuildContent(IReadOnlyList<string> lines)
        {
            var sb = new StringBuilder();
            sb.AppendLine("BT");
            sb.AppendLine("/F1 10 Tf");
            sb.AppendLine("50 800 Td");
            var first = true;
            foreach (var rawLine in lines.Take(58))
            {
                if (!first) sb.AppendLine("0 -13 Td");
                first = false;
                sb.Append('(').Append(EscapePdf(ToAscii(rawLine))).AppendLine(") Tj");
            }
            sb.AppendLine("ET");
            return Ascii(sb.ToString());
        }

        private static string ToAscii(string value)
        {
            var normalized = value.Normalize(NormalizationForm.FormD);
            var chars = normalized.Where(c => c <= 127).ToArray();
            return new string(chars).Replace('\t', ' ');
        }

        private static string EscapePdf(string value)
            => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

        private static byte[] Ascii(string value) => Encoding.ASCII.GetBytes(value);
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
