using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;
using School_Management_System.Models;
using School_Management_System.Options;

namespace School_Management_System.Services;

// A4 landscape, three detachable copies. Text and amounts are drawn from the ledger on every export.
public sealed class ReferenceChallanPdf(IWebHostEnvironment environment, IOptions<ChallanTemplateOptions> options)
{
    private static string N(decimal n) => n.ToString("0.##", CultureInfo.InvariantCulture);
    private static byte[] Bytes(string s) => Encoding.Latin1.GetBytes(s);
    public byte[] Create(School school, IReadOnlyList<ChallanPrintModel> models)
    {
        if (models.Count == 0) throw new InvalidOperationException("No challans match this selection.");
        var logo = ReadLogo(school.LogoPath);
        var objects = new List<byte[]> { Array.Empty<byte>(), Array.Empty<byte>(),
            Bytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"),
            Bytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>"),
            Bytes("<< /Type /Font /Subtype /Type1 /BaseFont /Times-Bold /Encoding /WinAnsiEncoding >>") };
        int imageId = 0;
        if (logo is not null)
        {
            imageId = objects.Count + 1;
            objects.Add(Stream(logo.Value.Data, $"/Type /XObject /Subtype /Image /Width {logo.Value.Width} /Height {logo.Value.Height} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode"));
        }
        var pages = new List<int>();
        foreach (var model in models)
        {
            var canvas = new Canvas();
            for (int i = 0; i < 3; i++) Draw(canvas, 30 + i * 260.6, school, model, new[] { "Bank Copy", "School Copy", "Student Copy" }[i], logo is not null);
            var contentId = objects.Count + 1;
            objects.Add(Stream(Bytes(canvas.ToString())));
            pages.Add(objects.Count + 1);
            objects.Add(Bytes($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 841.89 595.28] /Resources << /Font << /F1 3 0 R /F2 4 0 R /F3 5 0 R >> {(imageId > 0 ? $"/XObject << /Logo {imageId} 0 R >>" : "")} >> /Contents {contentId} 0 R >>"));
        }
        objects[0] = Bytes("<< /Type /Catalog /Pages 2 0 R >>");
        objects[1] = Bytes($"<< /Type /Pages /Count {pages.Count} /Kids [{string.Join(" ", pages.Select(i => $"{i} 0 R"))}] >>");
        using var output = new MemoryStream();
        output.Write(Bytes("%PDF-1.4\n%âãÏÓ\n"));
        var offsets = new List<long> { 0 };
        for (int i = 0; i < objects.Count; i++)
        {
            offsets.Add(output.Position); output.Write(Bytes($"{i + 1} 0 obj\n")); output.Write(objects[i]); output.Write(Bytes("\nendobj\n"));
        }
        long xref = output.Position;
        output.Write(Bytes($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n"));
        foreach (var offset in offsets.Skip(1)) output.Write(Bytes(offset.ToString("0000000000", CultureInfo.InvariantCulture) + " 00000 n \n"));
        output.Write(Bytes($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF"));
        return output.ToArray();
    }

    private void Draw(Canvas p, double x, School school, ChallanPrintModel m, string copy, bool logo)
    {
        const double w = 260.6;
        var c = m.Challan;
        p.Rect(x, 28, w, 470);
        p.Text(x + 5, 40, "Chalan No:", 8);
        p.Text(x + 62, 40, c.ChallanNumber, 8, maxWidth: 133);
        p.Text(x + 197, 40, copy, 8, maxWidth: 59);
        if (logo) p.Image(x + 6, 51, 38, 42);
        p.Text(x + 48, 72, school.Name, 18, "F3", 206);
        p.Text(x + 48, 85, options.Value.Motto, 6.8, "F2", 206);
        p.Line(x + 48, 88, x + 254, 88);
        p.Text(x + 5, 110, "Date of Issue: " + c.IssueDate.ToString("dd/MM/yyyy"), 7.6);
        p.Text(x + 153, 110, "Due Date: " + c.DueDate.ToString("dd/MM/yyyy"), 7.6, maxWidth: 102);
        p.Text(x + 5, 132, "Student Name:", 9);
        p.Text(x + 91, 132, c.Student.FullName, 9, maxWidth: 164);
        p.Text(x + 5, 147, "Father Name:", 9);
        p.Text(x + 91, 147, c.Student.FatherGuardianName ?? "-", 9, maxWidth: 164);
        p.Text(x + 5, 162, "Class:", 9);
        p.Text(x + 43, 162, $"{c.ClassNameSnapshot} {c.SectionNameSnapshot}".Trim(), 9, maxWidth: 90);
        p.Text(x + 138, 162, "Adm: " + c.Student.AdmissionNumber, 8, maxWidth: 117);
        p.Text(x + 5, 178, "Fee for the Month:", 9);
        p.Wrapped(x + 100, 177, m.Months, 8, 154, 10);
        p.Rect(x, 207, w, 35, .9);
        p.Text(x + 37, 221, "Account No: " + options.Value.BankAccountNumber, 9, maxWidth: 215);
        p.Text(x + 43, 234, options.Value.BankName, 9, maxWidth: 209);

        decimal tuition = 0, admission = 0, other = 0;
        foreach (var item in c.Items)
        {
            var name = item.Description.ToLowerInvariant();
            if (name.Contains("tuition") || name.Contains("monthly")) tuition += item.NetAmount;
            else if (name.Contains("admission") || name.Contains("annual")) admission += item.NetAmount;
            else other += item.NetAmount;
        }
        var rows = new (string Label, decimal? Value)[] {
            ("Admission Fee / Annual Fee", admission), ("Tuition Fee", tuition),
            ("Other Charges", other), ("Dues (previous unpaid months)", m.ArrearsTotal),
            ("Received (this month)", -c.PaidAmount), ("", null), ("", null),
            ("Payable Due Date", m.Payable), ("Payable After Due Date", m.Payable + m.AdditionalLateFee)
        };
        for (var i = 0; i < rows.Length; i++)
        {
            var y = 242 + i * 25;
            p.Line(x, y, x + w, y);
            p.Text(x + 7, y + 17, rows[i].Label, 9, i >= 7 ? "F2" : "F1", 183);
            if (rows[i].Value.HasValue) p.Text(x + 197, y + 17, N(rows[i].Value!.Value), 9, i >= 7 ? "F2" : "F1", 59);
        }
        p.Line(x, 467, x + w, 467); p.Line(x + 192, 242, x + 192, 467);
        p.Text(x + 5, 479, $"Balance as of {DateTime.Today:dd/MM/yyyy}. Discounts included.", 6.5, maxWidth: 249);
        p.Text(x + 5, 490, m.GraceDays > 0 ? $"Late fee after {m.GraceDays} grace day(s); existing policy applies." : "Existing late-fee policy applies; paid dues are excluded.", 6.5, maxWidth: 249);
        p.Line(x, 498, x, 555); p.Line(x + w, 498, x + w, 555);
        p.Text(x + 5, 550, "Accountant", 9); p.Text(x + 200, 550, "Cashier", 9);
        if (school.RegistrationNumber?.StartsWith("DEMO-", StringComparison.OrdinalIgnoreCase) == true)
            p.Text(x + 55, 571, "DEMO DATA - NOT FOR PAYMENT", 7, "F2");
    }

    private (byte[] Data, int Width, int Height)? ReadLogo(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        var root = Path.GetFullPath(environment.WebRootPath) + Path.DirectorySeparatorChar;
        var full = Path.GetFullPath(Path.Combine(root, path.TrimStart('~', '/', '\\').Replace('/', Path.DirectorySeparatorChar)));
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(full)) return null;
        var data = File.ReadAllBytes(full);
        // Original school logo is JPEG; never fetch remote resources while rendering financial documents.
        if (data.Length < 4 || data[0] != 255 || data[1] != 216) return null;
        for (var i = 2; i + 9 < data.Length;)
        {
            if (data[i++] != 255) continue;
            int marker = data[i++];
            if (marker == 255 || marker == 216) continue;
            if (marker == 217 || marker == 218) break;
            int length = (data[i] << 8) + data[i + 1];
            if (length < 2 || i + length > data.Length) break;
            if (marker is 192 or 193 or 194)
                return data[i + 7] == 3 ? (data, (data[i + 5] << 8) + data[i + 6], (data[i + 3] << 8) + data[i + 4]) : null;
            i += length;
        }
        return null;
    }

    private static byte[] Stream(byte[] content, string extra = "")
    {
        using var s = new MemoryStream(); s.Write(Bytes($"<< /Length {content.Length} {extra} >>\nstream\n"));
        s.Write(content); s.Write(Bytes("\nendstream")); return s.ToArray();
    }
    private sealed class Canvas
    {
        private readonly StringBuilder s = new("0.5 w\n0 G\n");
        private static string F(double n) => n.ToString("0.###", CultureInfo.InvariantCulture);
        public void Line(double x, double y, double xx, double yy) => s.AppendLine($"{F(x)} {F(595.28-y)} m {F(xx)} {F(595.28-yy)} l S");
        public void Rect(double x, double y, double w, double h, double? grey = null)
        {
            if (grey.HasValue) s.AppendLine($"q {F(grey.Value)} g {F(x)} {F(595.28-y-h)} {F(w)} {F(h)} re f Q");
            s.AppendLine($"{F(x)} {F(595.28-y-h)} {F(w)} {F(h)} re S");
        }
        public void Image(double x, double y, double w, double h) => s.AppendLine($"q {F(w)} 0 0 {F(h)} {F(x)} {F(595.28-y-h)} cm /Logo Do Q");
        public void Text(double x, double y, string text, double size, string font = "F1", double maxWidth = 1000)
        {
            // Horizontal scaling preserves the complete value rather than dropping names or monetary digits.
            var width = text.Sum(c => "ilI.,:! '".Contains(c) ? .27 : "MW@".Contains(c) ? .88 : .56) * size;
            var scale = Math.Min(100, maxWidth / Math.Max(1, width) * 100);
            var escaped = text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("\r", " ").Replace("\n", " ");
            s.AppendLine($"BT /{font} {F(size)} Tf {F(scale)} Tz 1 0 0 1 {F(x)} {F(595.28-y)} Tm ({escaped}) Tj ET");
        }
        public void Wrapped(double x, double y, string text, double size, double width, double leading)
        {
            var words = text.Split(' '); var line = ""; int count = 0;
            foreach (var word in words)
            {
                if ((line.Length + word.Length) * size * .52 > width && line.Length > 0 && count < 2)
                { Text(x, y, line, size, maxWidth: width); y += leading; count++; line = ""; }
                line += (line.Length > 0 ? " " : "") + word;
            }
            Text(x, y, line, size, maxWidth: width);
        }
        public override string ToString() => s.ToString();
    }
}
