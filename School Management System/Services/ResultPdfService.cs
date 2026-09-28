using System.Globalization;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using School_Management_System.Models;
using School_Management_System.ViewModels;
using Section = MigraDoc.DocumentObjectModel.Section;

namespace School_Management_System.Services;

public class ResultPdfService : IResultPdfService
{
    private readonly ISystemSettingsService _settings;
    private readonly IWebHostEnvironment _environment;
    private static readonly Color Green = Color.FromRgb(35, 99, 76);
    private static readonly Color Pale = Color.FromRgb(237, 245, 240);
    private static readonly Color Border = Color.FromRgb(211, 224, 216);
    static ResultPdfService() { GlobalFontSettings.UseWindowsFontsUnderWindows = true; }
    public ResultPdfService(ISystemSettingsService settings, IWebHostEnvironment environment)
    { _settings = settings; _environment = environment; }

    public async Task<byte[]> CreateResultCardPdfAsync(ResultCardViewModel model, CancellationToken cancellationToken = default)
        => await CreateBulkResultCardsPdfAsync([model], cancellationToken);

    public async Task<byte[]> CreateBulkResultCardsPdfAsync(IReadOnlyList<ResultCardViewModel> models, CancellationToken cancellationToken = default)
    {
        if (models.Count == 0) throw new InvalidOperationException("No published result cards are available.");
        var settings = await _settings.GetAsync(models[0].School.Id, cancellationToken);
        var document = CreateDocument(models[0].School.Name + " - Result cards");
        foreach (var model in models) { cancellationToken.ThrowIfCancellationRequested(); AddCard(document, model, settings); }
        return Render(document);
    }

    public async Task<byte[]> CreateClassResultSheetPdfAsync(ClassResultViewModel model, CancellationToken cancellationToken = default)
    {
        var document = CreateDocument(model.Exam.Title + " - Class result sheet");
        var section = AddSection(document);
        Heading(section, model.Exam.School?.Name ?? "School", "CLASS RESULT SHEET", model.Exam.Title);
        Text(section, model.SchoolClass.Name + " · Section: " + (model.Section?.Name ?? "All sections"));
        Text(section, "Positions compare percentages across the class. Equal percentages share a position.", 9);
        var table = Table(section, [1.1, 1.6, 5.4, 2.8, 1.9, 1.4, 2.0], ["Pos.", "Roll", "Student", "Marks", "%", "Grade", "Result"]);
        foreach (var result in model.Rows)
            Row(table, [result.Position?.ToString() ?? "-", result.RollNumber ?? "-", result.StudentName,
                N(result.ObtainedMarks) + " / " + N(result.MaximumMarks), result.Percentage.ToString("0.00", CultureInfo.InvariantCulture), result.Grade, result.IsPassed ? "PASS" : "FAIL"]);
        Footer(section, "Class result sheet · " + model.Exam.Title);
        var settings = await _settings.GetAsync(model.Exam.SchoolId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(settings.GeneralPrintFooterText)) Text(section, settings.GeneralPrintFooterText, 9);
        return Render(document);
    }

    private void AddCard(Document document, ResultCardViewModel model, SystemSetting settings)
    {
        var section = AddSection(document);
        var r = model.Result;
        var logo = LocalImage(model.School.LogoPath);
        if (logo is not null)
        {
            var p = section.AddParagraph(); p.Format.Alignment = ParagraphAlignment.Center;
            var image = p.AddImage(logo); image.Width = Unit.FromCentimeter(1.6); image.LockAspectRatio = true;
        }
        Heading(section, model.School.Name, "STUDENT REPORT CARD", model.Exam.Title);
        if (!string.IsNullOrWhiteSpace(model.School.Address)) Center(section, model.School.Address!, 9);
        if (!string.IsNullOrWhiteSpace(model.School.Phone)) Center(section, model.School.Phone!, 9);
        Center(section, "Academic session: " + (model.Exam.AcademicSession?.Name ?? "-"), 10);
        var identity = Table(section, [8.1, 8.1]);
        Row(identity, ["STUDENT\n" + model.Student.FullName, "ADMISSION NO. / ROLL\n" + model.Student.AdmissionNumber + " / " + (model.Enrollment.RollNumber ?? model.Student.RollNumber ?? "-")]);
        Row(identity, ["FATHER / GUARDIAN\n" + (model.Student.FatherGuardianName ?? "-"), "CLASS / SECTION\n" + model.Enrollment.ClassName + " / " + (model.Enrollment.SectionName ?? "-")]);
        Space(section, 8);
        var weighted = model.Subjects.Any(x => x.WeightagePercent != 100m);
        var table = Table(section, weighted ? [4.7, 2.0, 1.6, 2.4, 2.0, 3.5] : [6.4, 2.4, 2.0, 2.7, 2.7],
            weighted ? ["Subject", "Maximum", "Pass", "Obtained", "Weight %", "Result"] : ["Subject", "Maximum", "Pass", "Obtained", "Result"]);
        foreach (var subject in model.Subjects)
        {
            var obtained = subject.SpecialStatus == MarkSpecialStatus.Absent ? "Absent" : subject.SpecialStatus == MarkSpecialStatus.Exempt ? "Exempt" : subject.ObtainedMarks.HasValue ? N(subject.ObtainedMarks.Value) : "-";
            var status = subject.SpecialStatus == MarkSpecialStatus.Exempt ? "EXEMPT" : subject.Passed ? "PASS" : "FAIL";
            Row(table, weighted ? [subject.Subject, N(subject.MaximumMarks), N(subject.PassMarks), obtained, N(subject.WeightagePercent), status] : [subject.Subject, N(subject.MaximumMarks), N(subject.PassMarks), obtained, status]);
        }
        Space(section, 10);
        var totals = Table(section, [5.4, 5.4, 5.4], [weighted ? "Weighted total" : "Total marks", "Percentage", "Grade / result"]);
        Row(totals, [N(r.ObtainedMarks) + " / " + N(r.MaximumMarks), r.Percentage.ToString("0.00", CultureInfo.InvariantCulture) + "%", r.Grade + " / " + (r.IsPassed ? "PASS" : "FAIL")]);
        if (settings.ResultCardShowClassPosition)
            Text(section, "Class position: " + (r.ClassPosition?.ToString() ?? "-") + " / " + model.ClassCandidateCount + " published students", 10, true);
        Text(section, model.SchoolRankingComplete ? "School position: " + model.SchoolPosition + " / " + model.SchoolCandidateCount + " students (same exam, by percentage)" : "School position: pending publication of all classes", 10, true);
        if (settings.ResultCardShowAttendance) Text(section, "Attendance: " + (r.AttendancePercentage.HasValue ? r.AttendancePercentage.Value.ToString("0.00", CultureInfo.InvariantCulture) + "%" : "Not recorded"), 10);
        Text(section, "Equal percentages share a position. Exempt subjects are excluded from totals." + (weighted ? " Totals apply the subject weights shown above." : ""), 8);
        if (!string.IsNullOrWhiteSpace(r.TeacherRemarks)) Text(section, "Teacher's remarks: " + r.TeacherRemarks, 10);
        if (!string.IsNullOrWhiteSpace(r.CorrectionReason)) Text(section, "Correction note: " + r.CorrectionReason, 9);
        if (!string.IsNullOrWhiteSpace(settings.ResultCardFooterText)) Text(section, settings.ResultCardFooterText, 9);
        Space(section, 18);
        var signatures = Table(section, [8.1, 8.1]); signatures.Borders.Visible = false;
        var signatureRow = signatures.AddRow(); signatureRow.KeepWith = 1;
        Signature(signatureRow.Cells[0], settings.ClassTeacherSignaturePath);
        Signature(signatureRow.Cells[1], settings.PrincipalSignaturePath);
        var labels = signatures.AddRow();
        labels.Cells[0].AddParagraph(settings.ClassTeacherSignatureLabel);
        labels.Cells[1].AddParagraph(settings.PrincipalSignatureLabel + (string.IsNullOrWhiteSpace(model.School.PrincipalName) ? "" : "\n" + model.School.PrincipalName));
        Footer(section, model.Student.AdmissionNumber + " · " + model.Exam.Title + " · Version " + r.VersionNumber + " · Published " + r.PublishedAtUtc.ToString("dd MMM yyyy", CultureInfo.InvariantCulture));
    }

    private void Signature(Cell cell, string? path)
    {
        var file = LocalImage(path);
        if (file is not null) { var image = cell.AddImage(file); image.Height = Unit.FromCentimeter(.8); image.LockAspectRatio = true; }
        else cell.AddParagraph("____________________________");
    }
    private string? LocalImage(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        var root = Path.GetFullPath(_environment.WebRootPath);
        var file = Path.GetFullPath(Path.Combine(root, path.TrimStart('~', '/', '\\').Replace('/', Path.DirectorySeparatorChar)));
        if (!file.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !File.Exists(file)) return null;
        return new[] { ".png", ".jpg", ".jpeg" }.Contains(Path.GetExtension(file).ToLowerInvariant()) ? file : null;
    }
    private static Document CreateDocument(string title)
    {
        var doc = new Document(); doc.Info.Title = title;
        var style = doc.Styles[StyleNames.Normal]!; style.Font.Name = "Arial"; style.Font.Size = 10;
        style.Font.Color = Color.FromRgb(35, 52, 45); style.ParagraphFormat.SpaceAfter = Unit.FromPoint(5);
        return doc;
    }
    private static Section AddSection(Document doc)
    {
        var s = doc.AddSection(); s.PageSetup.PageFormat = PageFormat.A4;
        s.PageSetup.TopMargin = Unit.FromCentimeter(1.3); s.PageSetup.BottomMargin = Unit.FromCentimeter(1.6);
        s.PageSetup.LeftMargin = s.PageSetup.RightMargin = Unit.FromCentimeter(2.4);
        return s;
    }
    private static void Heading(Section s, string school, string kind, string exam)
    { Center(s, school, 21, true); Center(s, kind, 10, true); Center(s, exam, 14, true); }
    private static void Center(Section s, string value, int size, bool bold = false)
    { var p = Text(s, value, size, bold); p.Format.Alignment = ParagraphAlignment.Center; p.Format.Font.Color = Green; }
    private static Paragraph Text(Section s, string value, int size = 10, bool bold = false)
    { var p = s.AddParagraph(value); p.Format.Font.Size = size; p.Format.Font.Bold = bold; p.Format.SpaceBefore = 3; return p; }
    private static void Space(Section s, int points) { var p = s.AddParagraph(); p.Format.Font.Size = 1; p.Format.SpaceAfter = points; }
    private static Table Table(Section s, double[] widths, string[]? headers = null)
    {
        var table = s.AddTable(); table.Borders.Color = Border; table.Borders.Width = .5;
        table.TopPadding = table.BottomPadding = Unit.FromPoint(7);
        table.LeftPadding = table.RightPadding = Unit.FromPoint(7);
        foreach (var width in widths) table.AddColumn(Unit.FromCentimeter(width));
        if (headers is not null)
        {
            var row = Row(table, headers); row.HeadingFormat = true; row.Shading.Color = Green; row.Format.Font.Color = Colors.White; row.Format.Font.Bold = true;
        }
        return table;
    }
    private static Row Row(Table table, string[] values)
    {
        var row = table.AddRow(); row.VerticalAlignment = VerticalAlignment.Center;
        if (table.Rows.Count % 2 == 0) row.Shading.Color = Pale;
        for (var i = 0; i < values.Length; i++) row.Cells[i].AddParagraph(values[i]);
        return row;
    }
    private static void Footer(Section s, string value)
    {
        var p = s.Footers.Primary.AddParagraph(value); p.Format.Font.Size = 8; p.Format.Alignment = ParagraphAlignment.Center;
        p.AddText(" · Page "); p.AddPageField();
    }
    private static byte[] Render(Document doc)
    { var renderer = new PdfDocumentRenderer { Document = doc }; renderer.RenderDocument(); using var stream = new MemoryStream(); renderer.PdfDocument.Save(stream, false); return stream.ToArray(); }
    private static string N(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
