using System.Globalization;
using System.Text.Json;

namespace SensorLogAnalyzer;

public static class LogLoader
{
    public static List<SensorLogEntry> Load(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".csv" => LoadCsv(path),
            ".json" => LoadJson(path),
            _ => throw new NotSupportedException("Only CSV and JSON files are supported.")
        };
    }

    private static List<SensorLogEntry> LoadCsv(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length < 2)
            return [];

        var headers = ParseCsvLine(lines[0])
            .Select(NormalizeHeader)
            .ToList();

        int timestampIdx = FindHeaderIndex(headers, SensorLogEntry.CsvTimestampNames);
        int machineIdx = FindHeaderIndex(headers, SensorLogEntry.CsvMachineNames);
        int valueIdx = FindHeaderIndex(headers, SensorLogEntry.CsvValueNames);
        int statusIdx = FindHeaderIndex(headers, SensorLogEntry.CsvStatusNames);

        if (timestampIdx < 0 || valueIdx < 0)
        {
            throw new InvalidDataException("CSV must contain at least timestamp/time and value/reading columns.");
        }

        var result = new List<SensorLogEntry>();
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var cells = ParseCsvLine(lines[i]);
            if (cells.Count == 0) continue;

            string tsRaw = GetCell(cells, timestampIdx);
            string valRaw = GetCell(cells, valueIdx);

            if (!double.TryParse(valRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) &&
                !double.TryParse(valRaw, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            {
                continue;
            }

            var entry = new SensorLogEntry
            {
                Timestamp = SensorLogEntry.ParseTimestamp(tsRaw),
                MachineId = machineIdx >= 0 ? GetCell(cells, machineIdx) : "Unknown",
                Value = value,
                Status = statusIdx >= 0 ? GetCell(cells, statusIdx) : null
            };

            result.Add(entry);
        }

        return result;
    }

    private static List<SensorLogEntry> LoadJson(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("JSON must be an array of log objects.");

        var result = new List<SensorLogEntry>();
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            string? ts = GetPropertyValue(item, SensorLogEntry.CsvTimestampNames);
            string machine = GetPropertyValue(item, SensorLogEntry.CsvMachineNames) ?? "Unknown";
            string? valueRaw = GetPropertyValue(item, SensorLogEntry.CsvValueNames);
            string? status = GetPropertyValue(item, SensorLogEntry.CsvStatusNames);

            if (ts is null || valueRaw is null)
                continue;

            if (!double.TryParse(valueRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) &&
                !double.TryParse(valueRaw, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            {
                continue;
            }

            result.Add(new SensorLogEntry
            {
                Timestamp = SensorLogEntry.ParseTimestamp(ts),
                MachineId = machine,
                Value = value,
                Status = status
            });
        }

        return result;
    }

    private static string? GetPropertyValue(JsonElement element, IEnumerable<string> names)
    {
        foreach (var prop in element.EnumerateObject())
        {
            var norm = NormalizeHeader(prop.Name);
            if (names.Any(n => NormalizeHeader(n) == norm))
            {
                return prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString(),
                    JsonValueKind.Number => prop.Value.ToString(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => prop.Value.ToString()
                };
            }
        }
        return null;
    }

    private static int FindHeaderIndex(List<string> headers, IEnumerable<string> candidates)
    {
        var normalized = candidates.Select(NormalizeHeader).ToHashSet();
        for (int i = 0; i < headers.Count; i++)
        {
            if (normalized.Contains(headers[i]))
                return i;
        }
        return -1;
    }

    private static string NormalizeHeader(string s)
    {
        s = s.Trim().ToLowerInvariant();
        return new string(s.Where(char.IsLetterOrDigit).ToArray());
    }

    private static string GetCell(List<string> cells, int index)
    {
        if (index < 0 || index >= cells.Count) return string.Empty;
        return cells[index].Trim();
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        if (line.Length == 0)
            return result;

        var current = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result;
    }
}
