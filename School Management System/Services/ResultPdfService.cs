using System.Text;
using School_Management_System.Models;
using School_Management_System.ViewModels;

namespace School_Management_System.Services;

public class ResultPdfService : IResultPdfService
{
    private readonly ISystemSettingsService _settings;

    public ResultPdfService(ISystemSettingsService settings)
    {
        _settings = settings;
    }

    public async Task<byte[]> CreateResultCardPdfAsync(ResultCardViewModel model, CancellationToken cancellationToken = default)
    {
        var settings = await _settings.GetAsync(model.School.Id, cancellationToken);
        return SimplePdfWriter.Create([BuildResultCardPage(model, settings)]);
    }

    public async Task<byte[]> CreateBulkResultCardsPdfAsync(IReadOnlyList<ResultCardViewModel> models, CancellationToken cancellationToken = default)
    {
        if (models.Count == 0) return SimplePdfWriter.Create([]);
        var settings = await _settings.GetAsync(models[0].School.Id, cancellationToken);
        return SimplePdfWriter.Create(models.Select(x => BuildResultCardPage(x, settings)).ToList());
    }

    public async Task<byte[]> CreateClassResultSheetPdfAsync(ClassResultViewModel model, CancellationToken cancellationToken = default)
    {
        var settings = await _settings.GetAsync(model.Exam.SchoolId, cancellationToken);
        const int chunkSize = 38;
        var pages = new List<IReadOnlyList<string>>();
        var chunks = model.Rows.Chunk(chunkSize).ToList();
        if (chunks.Count == 0) chunks.Add([]);

        for (var pageIndex = 0; pageIndex < chunks.Count; pageIndex++)
        {
            var lines = new List<string>
            {
                model.Exam.School?.Name ?? "School Management System",
                "CLASS RESULT SHEET",
                $"Exam: {model.Exam.Title}    Class: {model.SchoolClass.Name}    Section: {model.Section?.Name ?? "All"}",
                $"Page {pageIndex + 1} of {chunks.Count}",
                "",
                "Pos  Roll       Student                         Obt/Max        %       Grade  Status",
                "--------------------------------------------------------------------------------"
            };

            foreach (var row in chunks[pageIndex])
                lines.Add($"{(row.Position?.ToString() ?? "-").PadRight(4)} {Fit(row.RollNumber ?? "-", 10),-10} {Fit(row.StudentName, 30),-30} {row.ObtainedMarks,6:0.##}/{row.MaximumMarks,-6:0.##} {row.Percentage,7:0.00} {Fit(row.Grade, 6),-6} {(row.IsPassed ? "PASS" : "FAIL")}");

            if (!string.IsNullOrWhiteSpace(settings.GeneralPrintFooterText))
                lines.AddRange(Wrap(settings.GeneralPrintFooterText!, 88));
            pages.Add(lines);
        }

        return SimplePdfWriter.Create(pages);
    }

    private static IReadOnlyList<string> BuildResultCardPage(ResultCardViewModel model, School_Management_System.Models.SystemSetting settings)
    {
        var r = model.Result;
        var lines = new List<string>
        {
            model.School.Name,
            "RESULT CARD",
            $"Exam: {model.Exam.Title}    Session: {model.Exam.AcademicSession?.Name ?? "-"}",
            $"Student: {model.Student.FullName}    Admission No: {model.Student.AdmissionNumber}",
            $"Class: {model.Enrollment.ClassName}    Section: {model.Enrollment.SectionName ?? "-"}    Roll No: {model.Enrollment.RollNumber ?? model.Student.RollNumber ?? "-"}",
            "",
            "Subject                              Max       Pass      Obtained    Status",
            "----------------------------------------------------------------------------"
        };

        foreach (var subject in model.Subjects)
        {
            var obtained = subject.SpecialStatus == MarkSpecialStatus.Absent ? "Absent"
                : subject.SpecialStatus == MarkSpecialStatus.Exempt ? "Exempt"
                : subject.ObtainedMarks?.ToString("0.##") ?? "-";
            var status = subject.SpecialStatus == MarkSpecialStatus.Exempt ? "EXEMPT" : subject.Passed ? "PASS" : "FAIL";
            lines.Add($"{Fit(subject.Subject, 36),-36} {subject.MaximumMarks,8:0.##} {subject.PassMarks,9:0.##} {obtained,12} {status,9}");
        }

        lines.Add("----------------------------------------------------------------------------");
        lines.Add($"Total: {r.ObtainedMarks:0.##} / {r.MaximumMarks:0.##}");
        lines.Add($"Percentage: {r.Percentage:0.00}%    Grade: {r.Grade}    Result: {(r.IsPassed ? "PASS" : "FAIL")}");
        var summaryParts = new List<string>();
        if (settings.ResultCardShowClassPosition) summaryParts.Add($"Class Position: {(r.ClassPosition?.ToString() ?? "-")}");
        if (settings.ResultCardShowAttendance) summaryParts.Add($"Attendance: {(r.AttendancePercentage.HasValue ? r.AttendancePercentage.Value.ToString("0.00") + "%" : "-")}");
        if (summaryParts.Count > 0) lines.Add(string.Join("    ", summaryParts));
        lines.Add($"Published: {r.PublishedAtUtc.ToLocalTime():dd MMM yyyy HH:mm}    Version: {r.VersionNumber}");
        if (!string.IsNullOrWhiteSpace(r.TeacherRemarks))
            lines.AddRange(Wrap("Teacher Remarks: " + r.TeacherRemarks, 88));
        if (!string.IsNullOrWhiteSpace(r.CorrectionReason))
            lines.AddRange(Wrap("Correction Note: " + r.CorrectionReason, 88));
        if (!string.IsNullOrWhiteSpace(settings.ResultCardFooterText))
            lines.AddRange(Wrap(settings.ResultCardFooterText!, 88));
        lines.Add("");
        lines.Add($"{settings.ClassTeacherSignatureLabel}: ____________________");
        lines.Add($"{settings.PrincipalSignatureLabel}: ____________________    {model.School.PrincipalName ?? string.Empty}");
        return lines;
    }

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
            objects[1] = Ascii("<< /Type /Catalog /Pages 2 0 R >>");
            var kidRefs = string.Join(' ', Enumerable.Range(0, pageCount).Select(i => $"{3 + (i * 2)} 0 R"));
            objects[2] = Ascii($"<< /Type /Pages /Count {pageCount} /Kids [{kidRefs}] >>");

            for (var i = 0; i < pageCount; i++)
            {
                var pageId = 3 + (i * 2);
                var contentId = pageId + 1;
                var content = BuildContent(pageList[i]);
                objects[pageId] = Ascii($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth:0} {PageHeight:0}] /Resources << /Font << /F1 {fontId} 0 R >> >> /Contents {contentId} 0 R >>");
                objects[contentId] = Combine(Ascii($"<< /Length {content.Length} >>\nstream\n"), content, Ascii("\nendstream"));
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
            Write(ms, Ascii($"xref\n0 {objectCount + 1}\n0000000000 65535 f \n"));
            for (var id = 1; id <= objectCount; id++) Write(ms, Ascii($"{offsets[id]:0000000000} 00000 n \n"));
            Write(ms, Ascii($"trailer\n<< /Size {objectCount + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF"));
            return ms.ToArray();
        }

        private static byte[] BuildContent(IReadOnlyList<string> lines)
        {
            var sb = new StringBuilder();
            sb.AppendLine("BT\n/F1 9 Tf\n40 805 Td");
            var first = true;
            foreach (var rawLine in lines.Take(62))
            {
                if (!first) sb.AppendLine("0 -12 Td");
                first = false;
                sb.Append('(').Append(EscapePdf(ToAscii(rawLine))).AppendLine(") Tj");
            }
            sb.AppendLine("ET");
            return Ascii(sb.ToString());
        }

        private static string ToAscii(string value)
        {
            var normalized = value.Normalize(NormalizationForm.FormD);
            return new string(normalized.Where(c => c <= 127).ToArray()).Replace('\t', ' ');
        }
        private static string EscapePdf(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        private static byte[] Ascii(string value) => Encoding.ASCII.GetBytes(value);
        private static byte[] Combine(params byte[][] arrays)
        {
            var result = new byte[arrays.Sum(x => x.Length)];
            var offset = 0;
            foreach (var array in arrays) { Buffer.BlockCopy(array, 0, result, offset, array.Length); offset += array.Length; }
            return result;
        }
        private static void Write(Stream stream, byte[] bytes) => stream.Write(bytes, 0, bytes.Length);
    }
}
