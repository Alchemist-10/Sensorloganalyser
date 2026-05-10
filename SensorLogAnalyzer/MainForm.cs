using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace SensorLogAnalyzer;

public sealed class MainForm : Form
{
    private readonly BindingList<SensorLogEntry> _allEntries = [];
    private readonly BindingList<SensorLogEntry> _filteredEntries = [];
    private readonly BindingSource _gridSource = new();

    private readonly PlotView _plotView = new();
    private readonly DataGridView _grid = new();
    private readonly ComboBox _machineFilter = new();
    private readonly ComboBox _severityFilter = new();
    private readonly DateTimePicker _startPicker = new();
    private readonly DateTimePicker _endPicker = new();
    private readonly Label _status = new();

    private readonly NumericUpDown _warningLow = new();
    private readonly NumericUpDown _warningHigh = new();
    private readonly NumericUpDown _criticalLow = new();
    private readonly NumericUpDown _criticalHigh = new();

    private string _currentFile = string.Empty;

    public MainForm()
    {
        Text = "Sensor Log Analyzer";
        Width = 1400;
        Height = 900;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);
        BackColor = Color.White;

        BuildUi();
        RefreshEmptyState();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var header = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
        root.Controls.Add(header, 0, 0);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 36,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        header.Controls.Add(buttons);

        var btnOpen = MakeButton("Open CSV/JSON");
        btnOpen.Click += (_, _) => OpenFile();
        buttons.Controls.Add(btnOpen);

        var btnExportCsv = MakeButton("Export CSV");
        btnExportCsv.Click += (_, _) => ExportCsv();
        buttons.Controls.Add(btnExportCsv);

        var btnExportPdf = MakeButton("Export PDF");
        btnExportPdf.Click += (_, _) => ExportPdf();
        buttons.Controls.Add(btnExportPdf);

        var btnRefresh = MakeButton("Refresh");
        btnRefresh.Click += (_, _) => ApplyFilters();
        buttons.Controls.Add(btnRefresh);

        var filters = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 88,
            ColumnCount = 8,
            RowCount = 2,
            Padding = new Padding(0, 8, 0, 0)
        };
        for (int i = 0; i < 8; i++)
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5f));
        filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        header.Controls.Add(filters);

        SetupCombo(_machineFilter);
        _severityFilter.DropDownStyle = ComboBoxStyle.DropDownList;
        _severityFilter.Items.AddRange(["All", "Normal", "Warning", "Critical"]);
        _severityFilter.SelectedIndex = 0;
        _severityFilter.SelectedIndexChanged += (_, _) => ApplyFilters();

        SetupDatePicker(_startPicker);
        SetupDatePicker(_endPicker);

        ConfigureThreshold(_warningLow);
        ConfigureThreshold(_warningHigh);
        ConfigureThreshold(_criticalLow);
        ConfigureThreshold(_criticalHigh);

        AddLabeled(filters, 0, 0, "Machine", _machineFilter);
        AddLabeled(filters, 1, 0, "Severity", _severityFilter);
        AddLabeled(filters, 2, 0, "Start", _startPicker);
        AddLabeled(filters, 3, 0, "End", _endPicker);
        AddLabeled(filters, 4, 0, "Warn Low", _warningLow);
        AddLabeled(filters, 5, 0, "Warn High", _warningHigh);
        AddLabeled(filters, 6, 0, "Critical Low", _criticalLow);
        AddLabeled(filters, 7, 0, "Critical High", _criticalHigh);

        _status.Dock = DockStyle.Bottom;
        _status.Height = 24;
        _status.TextAlign = ContentAlignment.MiddleLeft;
        _status.ForeColor = Color.DimGray;
        header.Controls.Add(_status);

        var body = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 420
        };
        root.Controls.Add(body, 0, 1);

        _plotView.Dock = DockStyle.Fill;
        _plotView.Model = new PlotModel { Title = "Sensor Time Series" };
        body.Panel1.Controls.Add(_plotView);

        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.DataSource = _gridSource;
        _grid.RowPrePaint += Grid_RowPrePaint;
        body.Panel2.Controls.Add(_grid);

        _machineFilter.SelectedIndexChanged += (_, _) => ApplyFilters();
        _startPicker.ValueChanged += (_, _) => ApplyFilters();
        _endPicker.ValueChanged += (_, _) => ApplyFilters();
        _warningLow.ValueChanged += (_, _) => ApplyFilters();
        _warningHigh.ValueChanged += (_, _) => ApplyFilters();
        _criticalLow.ValueChanged += (_, _) => ApplyFilters();
        _criticalHigh.ValueChanged += (_, _) => ApplyFilters();

        var menu = new MenuStrip();
        var fileMenu = new ToolStripMenuItem("File");
        fileMenu.DropDownItems.Add("Open CSV/JSON", null, (_, _) => OpenFile());
        fileMenu.DropDownItems.Add("Export CSV", null, (_, _) => ExportCsv());
        fileMenu.DropDownItems.Add("Export PDF", null, (_, _) => ExportPdf());
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add("Exit", null, (_, _) => Close());
        menu.Items.Add(fileMenu);
        MainMenuStrip = menu;
        Controls.Add(menu);
        menu.Dock = DockStyle.Top;
    }

    private static Button MakeButton(string text)
        => new()
        {
            Text = text,
            Width = 130,
            Height = 28,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(240, 244, 248)
        };

    private static void SetupCombo(ComboBox combo)
    {
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.Width = 160;
    }

    private static void SetupDatePicker(DateTimePicker picker)
    {
        picker.Width = 180;
        picker.Format = DateTimePickerFormat.Custom;
        picker.CustomFormat = "yyyy-MM-dd HH:mm";
        picker.ShowCheckBox = true;
        picker.Checked = false;
    }

    private static void ConfigureThreshold(NumericUpDown nud)
    {
        nud.DecimalPlaces = 2;
        nud.Maximum = 1000000;
        nud.Minimum = -1000000;
        nud.Width = 140;
    }

    private void AddLabeled(TableLayoutPanel panel, int col, int row, string label, Control control)
    {
        var cell = new Panel { Dock = DockStyle.Fill };
        var lbl = new Label
        {
            Text = label,
            Dock = DockStyle.Top,
            Height = 16,
            ForeColor = Color.DimGray
        };
        control.Dock = DockStyle.Bottom;
        cell.Controls.Add(control);
        cell.Controls.Add(lbl);
        panel.Controls.Add(cell, col, row);
    }

    private void OpenFile()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Sensor logs (*.csv;*.json)|*.csv;*.json|CSV (*.csv)|*.csv|JSON (*.json)|*.json|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var loaded = LogLoader.Load(dialog.FileName);
            _currentFile = dialog.FileName;

            _allEntries.Clear();
            foreach (var entry in loaded.OrderBy(x => x.Timestamp))
                _allEntries.Add(entry);

            if (_allEntries.Count == 0)
            {
                MessageBox.Show(this, "The selected file contained no usable log rows.", "No data", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshEmptyState();
                return;
            }

            var (wLow, wHigh, cLow, cHigh) = AnomalyDetector.FromData(_allEntries.ToList());
            _warningLow.Value = (decimal)Math.Round(wLow, 2);
            _warningHigh.Value = (decimal)Math.Round(wHigh, 2);
            _criticalLow.Value = (decimal)Math.Round(cLow, 2);
            _criticalHigh.Value = (decimal)Math.Round(cHigh, 2);

            PopulateFilters();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Load failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void PopulateFilters()
    {
        _machineFilter.BeginUpdate();
        _machineFilter.Items.Clear();
        _machineFilter.Items.Add("All");
        foreach (var machine in _allEntries.Select(e => e.MachineId).Distinct().OrderBy(s => s))
            _machineFilter.Items.Add(machine);
        _machineFilter.SelectedIndex = 0;
        _machineFilter.EndUpdate();

        _startPicker.MinDate = _allEntries.Min(e => e.Timestamp).Date;
        _endPicker.MaxDate = _allEntries.Max(e => e.Timestamp).AddDays(1);

        _startPicker.Value = _allEntries.Min(e => e.Timestamp).Date;
        _endPicker.Value = _allEntries.Max(e => e.Timestamp);
        _startPicker.Checked = false;
        _endPicker.Checked = false;

        _status.Text = $"Loaded {_allEntries.Count:n0} rows from {Path.GetFileName(_currentFile)}";
    }

    private void ApplyFilters()
    {
        if (_allEntries.Count == 0)
            return;

        var machine = _machineFilter.SelectedItem?.ToString() ?? "All";
        var severity = _severityFilter.SelectedItem?.ToString() ?? "All";
        bool startEnabled = _startPicker.Checked;
        bool endEnabled = _endPicker.Checked;
        DateTime start = _startPicker.Value;
        DateTime end = _endPicker.Value;

        var warningLow = (double)_warningLow.Value;
        var warningHigh = (double)_warningHigh.Value;
        var criticalLow = (double)_criticalLow.Value;
        var criticalHigh = (double)_criticalHigh.Value;

        var working = _allEntries
            .Where(e => machine == "All" || e.MachineId == machine)
            .Where(e => !startEnabled || e.Timestamp >= start)
            .Where(e => !endEnabled || e.Timestamp <= end)
            .ToList();

        AnomalyDetector.Evaluate(working, warningLow, warningHigh, criticalLow, criticalHigh);

        if (severity != "All" && Enum.TryParse<AnomalySeverity>(severity, out var selectedSeverity))
            working = working.Where(e => e.Severity == selectedSeverity).ToList();

        _filteredEntries.Clear();
        foreach (var e in working.OrderBy(x => x.Timestamp))
            _filteredEntries.Add(e);

        _gridSource.DataSource = _filteredEntries;
        BuildChart(_filteredEntries.ToList());
        UpdateStatus();
    }

    private void BuildChart(List<SensorLogEntry> entries)
    {
        var model = new PlotModel
        {
            Title = entries.Count == 0 ? "No data" : "Sensor Time Series"
        };

        model.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            StringFormat = "MM-dd HH:mm",
            Title = "Time",
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot
        });

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Value",
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot
        });

        if (entries.Count > 0)
        {
            var line = new LineSeries
            {
                Title = "Value",
                StrokeThickness = 2,
                MarkerType = MarkerType.Circle,
                MarkerSize = 3,
                Color = OxyColors.SteelBlue
            };

            foreach (var e in entries)
                line.Points.Add(DateTimeAxis.CreateDataPoint(e.Timestamp, e.Value));

            model.Series.Add(line);

            var warnings = new ScatterSeries
            {
                Title = "Warning",
                MarkerType = MarkerType.Circle,
                MarkerFill = OxyColors.Gold,
                MarkerStroke = OxyColors.Goldenrod,
                MarkerSize = 4
            };

            var criticals = new ScatterSeries
            {
                Title = "Critical",
                MarkerType = MarkerType.Triangle,
                MarkerFill = OxyColors.IndianRed,
                MarkerStroke = OxyColors.DarkRed,
                MarkerSize = 5
            };

            foreach (var e in entries.Where(x => x.Severity == AnomalySeverity.Warning))
                warnings.Points.Add(new ScatterPoint(DateTimeAxis.ToDouble(e.Timestamp), e.Value));

            foreach (var e in entries.Where(x => x.Severity == AnomalySeverity.Critical))
                criticals.Points.Add(new ScatterPoint(DateTimeAxis.ToDouble(e.Timestamp), e.Value));

            if (warnings.Points.Count > 0) model.Series.Add(warnings);
            if (criticals.Points.Count > 0) model.Series.Add(criticals);
        }

        _plotView.Model = model;
    }

    private void UpdateStatus()
    {
        int warnings = _filteredEntries.Count(e => e.Severity == AnomalySeverity.Warning);
        int criticals = _filteredEntries.Count(e => e.Severity == AnomalySeverity.Critical);
        _status.Text = $"Showing {_filteredEntries.Count:n0} rows | Warning: {warnings:n0} | Critical: {criticals:n0}";
    }

    private void ExportCsv()
    {
        if (_filteredEntries.Count == 0)
        {
            MessageBox.Show(this, "No filtered data to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            FileName = "sensor-log-filtered.csv"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            ExportService.ExportCsv(dialog.FileName, _filteredEntries);
            MessageBox.Show(this, "CSV export complete.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExportPdf()
    {
        if (_filteredEntries.Count == 0)
        {
            MessageBox.Show(this, "No filtered data to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = "sensor-log-filtered.pdf"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            ExportService.ExportPdf(dialog.FileName, _filteredEntries, "Sensor Log Analyzer - Filtered Report");
            MessageBox.Show(this, "PDF export complete.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Grid_RowPrePaint(object? sender, DataGridViewRowPrePaintEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _filteredEntries.Count)
            return;

        var row = _grid.Rows[e.RowIndex];
        var item = _filteredEntries[e.RowIndex];

        row.DefaultCellStyle.BackColor = item.Severity switch
        {
            AnomalySeverity.Critical => Color.MistyRose,
            AnomalySeverity.Warning => Color.LightYellow,
            _ => Color.White
        };
    }

    private void RefreshEmptyState()
    {
        _gridSource.DataSource = null;
        _grid.DataSource = _gridSource;
        BuildChart(new List<SensorLogEntry>());
        _status.Text = "Open a CSV or JSON log to begin.";
    }
}
