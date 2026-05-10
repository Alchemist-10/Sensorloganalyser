namespace SensorLogAnalyzer;

public static class AnomalyDetector
{
    public static void Evaluate(
        List<SensorLogEntry> entries,
        double warningBelow,
        double warningAbove,
        double criticalBelow,
        double criticalAbove)
    {
        foreach (var e in entries)
        {
            e.Severity = AnomalySeverity.Normal;

            if (e.Value <= criticalBelow || e.Value >= criticalAbove)
                e.Severity = AnomalySeverity.Critical;
            else if (e.Value <= warningBelow || e.Value >= warningAbove)
                e.Severity = AnomalySeverity.Warning;
        }
    }

    public static (double warningBelow, double warningAbove, double criticalBelow, double criticalAbove) FromData(List<SensorLogEntry> entries)
    {
        if (entries.Count == 0)
            return (0, 0, 0, 0);

        var values = entries.Select(e => e.Value).ToList();
        double mean = values.Average();
        double sd = StandardDeviation(values);

        // Sensible defaults for equipment logs: yellow at ~1.5σ, red at ~2.5σ.
        double warningOffset = Math.Max(sd * 1.5, Math.Abs(mean) * 0.05);
        double criticalOffset = Math.Max(sd * 2.5, Math.Abs(mean) * 0.10);

        return (mean - warningOffset, mean + warningOffset, mean - criticalOffset, mean + criticalOffset);
    }

    private static double StandardDeviation(IReadOnlyCollection<double> values)
    {
        if (values.Count < 2) return 0;
        double avg = values.Average();
        double variance = values.Sum(v => Math.Pow(v - avg, 2)) / (values.Count - 1);
        return Math.Sqrt(variance);
    }
}
