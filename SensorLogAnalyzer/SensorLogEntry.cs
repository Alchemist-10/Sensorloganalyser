using System.Globalization;

namespace SensorLogAnalyzer;

public enum AnomalySeverity
{
    Normal,
    Warning,
    Critical
}

public sealed class SensorLogEntry
{
    public DateTime Timestamp { get; set; }
    public string MachineId { get; set; } = string.Empty;
    public double Value { get; set; }
    public string? Status { get; set; }

    public AnomalySeverity Severity { get; set; } = AnomalySeverity.Normal;

    public static readonly string[] CsvTimestampNames = ["timestamp", "time", "date", "datetime"];
    public static readonly string[] CsvMachineNames = ["machine", "machineid", "machine_id", "equipment", "asset", "device"];
    public static readonly string[] CsvValueNames = ["value", "sensorvalue", "reading", "measurement", "temp", "pressure", "vibration"];
    public static readonly string[] CsvStatusNames = ["status", "severity", "level", "flag"];

    public static DateTime ParseTimestamp(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new FormatException("Timestamp is empty.");

        raw = raw.Trim();

        string[] formats =
        [
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ssZ",
            "MM/dd/yyyy HH:mm:ss",
            "dd/MM/yyyy HH:mm:ss",
            "yyyy-MM-dd HH:mm",
            "MM/dd/yyyy HH:mm",
            "O"
        ];

        if (DateTime.TryParseExact(raw, formats, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out var dt))
        {
            return dt;
        }

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dt))
            return dt;

        if (DateTime.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out dt))
            return dt;

        throw new FormatException($"Could not parse timestamp: {raw}");
    }
}
