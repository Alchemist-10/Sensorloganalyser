# Sensor Log Analyzer

A Windows desktop app built in C# WinForms for viewing equipment sensor logs.

## What it does

- Loads sensor logs from CSV or JSON
- Plots the selected machine's time series in a chart
- Highlights anomalies:
  - red = critical
  - yellow = warning
- Exports filtered data to CSV or PDF

## Tech stack

- C# / .NET 8 WinForms
- OxyPlot.WinForms for charting

## Expected input format

### CSV
Use columns similar to:

```csv
Timestamp,MachineId,Value,Status
2026-05-10 09:00:00,Compressor-1,51.2,OK
2026-05-10 09:05:00,Compressor-1,67.8,Warning
```

### JSON
Array of objects like:

```json
[
  {
    "Timestamp": "2026-05-10T09:00:00",
    "MachineId": "Compressor-1",
    "Value": 51.2,
    "Status": "OK"
  }
]
```

## Build steps

1. Open `SensorLogAnalyzer.csproj` in Visual Studio.
2. Restore NuGet packages.
3. Run the app.
4. Open a CSV or JSON file.

## Notes

- The app auto-computes default warning/critical thresholds from the loaded data.
- You can override thresholds using the numeric inputs.
- PDF export uses a minimal built-in PDF writer so you do not need another PDF library.

## GitHub README idea

Add:
- screenshots of the app after loading data
- sample CSV / JSON logs
- a short demo GIF
- a note explaining anomaly detection logic
