# SensorLogAnalyzer

A professional Windows Desktop application built with C# and .NET 8 WinForms for analyzing equipment and machine sensor logs. **SensorLogAnalyzer** helps system operators and engineers load, visualize, and identify statistical anomalies in sensor data outputs automatically.

## Features

- **Multi-format Log Ingestion**: Import your machine logs seamlessly. Supports both natively structured CSV and JSON log sets.
- **Dynamic Anomaly Detection**: Automatically computes thresholds and standard deviations from your raw data. Highlights items out-of-band:
  - **Yellow (Warning)**: Triggers at approximately 1.5σ (Standard Deviations).
  - **Red (Critical)**: Triggers at approximately 2.5σ (Standard Deviations).
- **Customizable Thresholds**: Fine-tune or manually override computed values for Critical and Warning levels via the UI.
- **Data Visualization**: Integrated interactive charting powered by [OxyPlot](https://oxyplot.github.io/), displaying your telemetry over time.
- **Data Exporting**: Export filtered datasets or anomaly segments to CSV or PDF layout documents using built-in minimalist PDF processing without third-party bloatware.

## Technology Stack

- **Framework**: .NET 8.0 (Windows Forms)
- **Language**: C# 12
- **Charting**: OxyPlot.WindowsForms
- **Data Processing**: Native System.Text.Json / CSV Parsing logic

## Prerequisites

- [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- Visual Studio 2022 (v17.8+)

## Installation & Build

1. Clone or download this repository.
2. Open SensorLogAnalyzer.sln using Visual Studio 2022.
3. Once the project loads, dependencies including OxyPlot.WindowsForms will be restored automatically via NuGet.
4. Press F5 or click **Start** to build and launch the application.

## Expected Log Formats

### CSV Array Example
The required columns are Timestamp/Time and Value/Reading columns.

`csv
Timestamp,MachineId,Value,Status
2026-05-10 09:00:00,Compressor-1,51.2,OK
2026-05-10 09:05:00,Compressor-1,67.8,Warning
2026-05-10 09:10:00,Compressor-1,102.5,Critical
`

### JSON Array Example
Expects a root JSON array comprising structured objects mapping properties correctly.

`json
[
  {
    "Timestamp": "2026-05-10T09:00:00",
    "MachineId": "Compressor-1",
    "Value": 51.2,
    "Status": "OK"
  },
  {
    "Timestamp": "2026-05-10T09:10:00",
    "MachineId": "Compressor-1",
    "Value": 102.5,
    "Status": "Critical"
  }
]
`

## How It Works underneath:

When sensor records are loaded, the \AnomalyDetector\ parses the numeric value of each component log and captures the moving arithmetic mean. Through statistical variance parsing (Σ (x - μ)² / (N - 1)), it allocates the normal bell curve to set warning blocks for sensor readings indicating potential faults quickly.

## Project Structure

- AnomalyDetector.cs - Handles Standard Deviation computations, statistical allocations, and log threshold setting checks.
- LogLoader.cs - File and JSON/CSV format parsing, ensuring correct type mapping and CultureInfo invariants to map telemetry dynamically.
- MainForm.cs - Primary WinForms logic encompassing User Interface operations and Chart plotting.
- ExportService.cs - Custom implementations to convert subset log entries to downstream PDF/CSV documents.
- SensorLogEntry.cs - The entity structure that governs single Log telemetry inputs.

