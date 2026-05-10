using System.Text;
using System.Globalization;

namespace SensorLogAnalyzer;

public static class ExportService
{
    public static void ExportCsv(string path, IEnumerable<SensorLogEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,MachineId,Value,Status,Severity");

        foreach (var e in entries)
        {
            sb.AppendLine(string.Join(",",
                Csv(e.Timestamp.ToString("O")),
                Csv(e.MachineId),
                Csv(e.Value.ToString(CultureInfo.InvariantCulture)),
                Csv(e.Status ?? string.Empty),
                Csv(e.Severity.ToString())));
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    public static void ExportPdf(string path, IEnumerable<SensorLogEntry> entries, string title)
    {
        var rows = entries.ToList();
        var lines = new List<string>
        {
            title,
            $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"Rows: {rows.Count}",
            string.Empty,
            "Timestamp | MachineId | Value | Status | Severity"
        };

        foreach (var e in rows.Take(80))
        {
            lines.Add($"{e.Timestamp:yyyy-MM-dd HH:mm:ss} | {Trim(e.MachineId, 18),-18} | {e.Value,10:0.###} | {Trim(e.Status ?? string.Empty, 12),-12} | {e.Severity}");
        }

        if (rows.Count > 80)
            lines.Add($"... truncated after 80 of {rows.Count} rows ...");

        WriteSimplePdf(path, lines);
    }

    private static string Trim(string s, int max)
        => s.Length <= max ? s : s[..Math.Max(0, max - 1)] + "…";

    private static string Csv(string value)
    {
        value = value.Replace("\"", "\"\"");
        return $"\"{value}\"";
    }

    // Minimal raw PDF writer using Helvetica. Good enough for exporting simple reports without extra dependencies.
    private static void WriteSimplePdf(string path, IReadOnlyList<string> lines)
    {
        static string Escape(string s) => s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

        var content = new StringBuilder();
        content.AppendLine("BT");
        content.AppendLine("/F1 11 Tf");
        content.AppendLine("72 770 Td");

        int printed = 0;
        foreach (var line in lines)
        {
            var safe = Escape(line);
            if (printed == 0)
                content.AppendLine($"({safe}) Tj");
            else
                content.AppendLine($"0 -14 Td ({safe}) Tj");
            printed++;
        }
        content.AppendLine("ET");
        byte[] contentBytes = Encoding.ASCII.GetBytes(content.ToString());

        var objects = new List<string>();
        objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
        objects.Add("<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        objects.Add("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>");
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        objects.Add($"<< /Length {contentBytes.Length} >>\nstream\n{content}\nendstream");

        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, Encoding.ASCII, leaveOpen: true);

        writer.Write("%PDF-1.4\n");
        writer.Flush();

        var offsets = new List<long> { 0 };
        for (int i = 0; i < objects.Count; i++)
        {
            offsets.Add(ms.Position);
            writer.Write($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            writer.Flush();
        }

        long xref = ms.Position;
        writer.Write("xref\n");
        writer.Write($"0 {objects.Count + 1}\n");
        writer.Write("0000000000 65535 f \n");
        for (int i = 1; i < offsets.Count; i++)
            writer.Write($"{offsets[i]:0000000000} 00000 n \n");
        writer.Write("trailer\n");
        writer.Write($"<< /Size {objects.Count + 1} /Root 1 0 R >>\n");
        writer.Write("startxref\n");
        writer.Write($"{xref}\n");
        writer.Write("%%EOF");
        writer.Flush();

        File.WriteAllBytes(path, ms.ToArray());
    }
}
