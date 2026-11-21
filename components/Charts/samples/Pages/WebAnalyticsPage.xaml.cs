// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Globalization;
using JPSoftworks.DesktopToolkit.Charts;
using JPSoftworks.DesktopToolkit.Controls;
using JPSoftworks.DesktopToolkit.Controls.Demo.Controls;
using JPSoftworks.DesktopToolkit.Controls.Demo.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using static JPSoftworks.DesktopToolkit.Controls.Demo.Services.WebAnalyticsSample;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Pages;

public sealed partial class WebAnalyticsPage : Page
{
    private static readonly Color PreviousColor = Color.FromArgb(255, 134, 144, 167);
    private static readonly Color[] DeviceColors = [SamplePalette.Teal, SamplePalette.Purple];
    private static readonly ChartColorSet ActivityRamp = ChartPalette.CreateRamp(SamplePalette.Teal, 5);
    private static readonly double[] ActivityStops = [0, 30, 60, 110, 190];
    private static readonly string[] TrendTitles = ["Daily sessions", "Daily engagement rate", "Daily purchases", "Daily purchase rate"];

    private readonly ToggleButton[] _metricButtons;
    private readonly LineChart[] _sparklines;
    private readonly FrameworkElement[] _reportPanels;
    private AnalyticsPeriod? _current;
    private AnalyticsPeriod? _previous;
    private int _selectedMetric;
    private bool _ready;
    private bool _compact;

    private int SelectedReport => this.ReportTabs.Items.IndexOf(this.ReportTabs.SelectedItem);

    public WebAnalyticsPage()
    {
        this.InitializeComponent();
        this.PageHeader.SetLinks([SamplePageHeader.Documentation, SamplePageHeader.Source, SamplePageHeader.NuGetPackage]);
        this._metricButtons = [this.SessionsMetric, this.EngagementMetric, this.PurchasesMetric, this.PurchaseRateMetric];
        this._sparklines = [this.SessionsSparkline, this.EngagementSparkline, this.PurchasesSparkline, this.PurchaseRateSparkline];
        this._reportPanels = [this.OverviewReport, this.AcquisitionReport, this.EngagementReport, this.PurchaseReport];
        string[] channels = ["All channels", .. Channels];
        this.AnalyticsChannelBox.ItemsSource = channels;
        this.AnalyticsChannelBox.SelectedIndex = 0;
        this.ReportTabs.SelectedItem = this.OverviewTab;
        this.ReportDates.MinimumDate = FirstDay;
        this.ReportDates.MaximumDate = LastDay;
        this.ReportDates.Presets =
        [
            new("Last 7 days", new DateRange(LastDay.AddDays(-6), LastDay)),
            new("Last 14 days", new DateRange(LastDay.AddDays(-13), LastDay)),
            new("Last 28 days", new DateRange(FirstDay, LastDay)),
        ];
        this.ReportDates.Selection = new DateRangeSelection(new DateRange(FirstDay, LastDay), true);

        this.SessionHistory.XAxis = new ChartAxisOptions(tickLabelFormat: "d MMM");
        foreach (var chart in new Control[]
                 {
                     this.SessionHistory, this.DeviceMix, this.SessionChannels, this.VisitorHours, this.CheckoutJourney
                 })
        {
            ChartContextMenu.SetProvider(chart, ChartDetailsMenuProvider.Instance);
        }

        this.MobileSwatch.Background = new SolidColorBrush(DeviceColors[0]);
        this.DesktopSwatch.Background = new SolidColorBrush(DeviceColors[1]);
        this.VisitorHours.ActualThemeChanged += (_, _) => this.UpdateColorScale();
        this.UpdateColorScale();

        this._ready = true;
        this.UpdateLayout(this.Workspace.ActualWidth);
        this.ApplyFilters();
    }

    private void OnReportChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (!this._ready || this.SelectedReport < 0)
        {
            return;
        }

        for (var index = 0; index < this._reportPanels.Length; index++)
        {
            this._reportPanels[index].Visibility = this.SelectedReport == index ? Visibility.Visible : Visibility.Collapsed;
        }

        this.PresentReport();
        this.ReportScroll.ChangeView(null, 0, null, disableAnimation: true);
    }

    private void OnOpenAcquisitionClick(object sender, RoutedEventArgs args)
    {
        this.ReportTabs.SelectedItem = this.AcquisitionTab;
    }

    private void OnFilterChanged(object sender, SelectionChangedEventArgs args)
    {
        if (this._ready)
        {
            this.ApplyFilters();
        }
    }

    private void OnReportDatesChanged(object? sender, EventArgs args)
    {
        if (this._ready)
        {
            this.ApplyFilters();
        }
    }

    private void ApplyFilters()
    {
        if (this.ReportDates.Selection is not { } selection)
        {
            return;
        }

        var device = this.AnalyticsDeviceBox.SelectedIndex - 1;
        var channel = this.AnalyticsChannelBox.SelectedIndex - 1;
        this._current = CreatePeriod(selection.Range.StartDate, selection.Range.EndDate, device, channel);
        this._previous = selection.ComparisonRange is { } range ? CreatePeriod(range.StartDate, range.EndDate, device, channel) : null;
        this.PresentReport();
    }

    private void PresentReport()
    {
        if (this._current is not { } current)
        {
            return;
        }

        switch (this.SelectedReport)
        {
            case 0:
                this.ShowOverview(current);
                break;
            case 1:
                this.ShowAcquisition(current);
                break;
            case 2:
                this.VisitorHours.Data = CreateActivityData(current);
                this.UpdateLandingTable(current);
                break;
            case 3:
                this.ShowCheckout(current);
                break;
        }
    }

    private void ShowOverview(AnalyticsPeriod current)
    {
        var totals = current.Total;
        var baseline = this._previous?.Total;
        this.SessionsValue.Text = $"{totals.Sessions:N0}";
        this.EngagementValue.Text = $"{totals.EngagementRate:P1}";
        this.PurchasesValue.Text = $"{totals.Purchases:N0}";
        this.PurchaseRateValue.Text = $"{totals.PurchaseRate:P1}";

        this.SessionsDelta.Text = baseline is { } previous
            ? CountChange(totals.Sessions, previous.Sessions)
            : $"{(double)totals.Sessions / current.Daily.Length:N0} per day";
        this.EngagementDelta.Text = baseline is { } previousEngagement
            ? RateChange(totals.EngagementRate, previousEngagement.EngagementRate)
            : $"{totals.Engaged:N0} engaged sessions";
        this.PurchasesDelta.Text = baseline is { } previousPurchases
            ? CountChange(totals.Purchases, previousPurchases.Purchases)
            : $"{(double)totals.Purchases / current.Daily.Length:N0} per day";
        this.PurchaseRateDelta.Text = baseline is { } previousRate
            ? RateChange(totals.PurchaseRate, previousRate.PurchaseRate)
            : "Share of sessions";

        this.SetDeltaColor(this.SessionsDelta, totals.Sessions - baseline?.Sessions);
        this.SetDeltaColor(this.EngagementDelta, totals.EngagementRate - baseline?.EngagementRate);
        this.SetDeltaColor(this.PurchasesDelta, totals.Purchases - baseline?.Purchases);
        this.SetDeltaColor(this.PurchaseRateDelta, totals.PurchaseRate - baseline?.PurchaseRate);
        for (var index = 0; index < this._sparklines.Length; index++)
        {
            this._sparklines[index].Data = new LineChartData(
            [
                new LineChartSeries(
                    TrendTitles[index], MetricPoints(current, current.FirstDay, index), SamplePalette.Blue, SamplePalette.BlueFill)
            ]);
        }

        this.ShowTrend(current);
        this.DeviceMix.Data = new DoughnutGraphData(
            [new(Devices[0], DeviceColors[0]), new(Devices[1], DeviceColors[1])],
            [current.ByDevice[0].Sessions, current.ByDevice[1].Sessions]);
        this.DeviceMix.CenterValue = $"{totals.Sessions:N0}";
        this.MobileShare.Text = FormatShare(current.ByDevice[0].Sessions, totals.Sessions);
        this.DesktopShare.Text = FormatShare(current.ByDevice[1].Sessions, totals.Sessions);
        var topChannel = Array.IndexOf(current.ByChannel, current.ByChannel.MaxBy(item => item.Sessions));
        var topSessions = current.ByChannel[topChannel].Sessions;
        this.ChannelSummary.Text
            = $"{Channels[topChannel]} brought {topSessions:N0} sessions ({FormatShare(topSessions, totals.Sessions)}).";
    }

    private void SetDeltaColor(TextBlock label, double? change)
    {
        label.Style = (Style)this.Resources[change is > 0 ? "MetricGain" : change is < 0 ? "MetricLoss" : "ReportCaption"];
    }

    private static string CountChange(int count, int baseline)
    {
        return baseline > 0 ? $"{(double)(count - baseline) / baseline:+0.0%;-0.0%;0.0%} vs previous" : "No comparison baseline";
    }

    private static string RateChange(double rate, double baseline)
    {
        return $"{(rate - baseline) * 100:+0.0;-0.0;0.0} pp vs previous";
    }

    private static string FormatShare(double count, double total)
    {
        return total > 0 ? $"{count / total:P1}" : "0%";
    }

    private void OnMetricCheckChanged(object sender, RoutedEventArgs args)
    {
        if (!this._ready || this._current is not { } current)
        {
            return;
        }

        var metric = (ToggleButton)sender;
        var index = Array.IndexOf(this._metricButtons, metric);
        if (index == this._selectedMetric)
        {
            metric.IsChecked = true;
        }
        else if (metric.IsChecked == true)
        {
            this._selectedMetric = index;
            this.ShowTrend(current);
        }
    }

    private void ShowTrend(AnalyticsPeriod current)
    {
        for (var index = 0; index < this._metricButtons.Length; index++)
        {
            this._metricButtons[index].IsChecked = index == this._selectedMetric;
        }

        var rate = this._selectedMetric is 1 or 3;
        this.TrendHeading.Text = TrendTitles[this._selectedMetric];
        this.TrendPeriod.Text = this.FormatComparison(current);
        this.SessionHistory.ResetViewport();
        this.SessionHistory.ValueFormat = rate ? "0.#'%'" : "N0";
        this.SessionHistory.YAxis = rate ? new ChartAxisOptions(minimum: 0, tickLabelFormat: "0.#'%'") : new ChartAxisOptions(minimum: 0);
        var currentLine = new LineChartSeries(
            "Selected period", MetricPoints(current, current.FirstDay, this._selectedMetric), SamplePalette.Blue, SamplePalette.BlueFill);
        this.SessionHistory.Data = this._previous is { } previous
            ? new LineChartData(
            [
                currentLine,
                new LineChartSeries("Previous period", MetricPoints(previous, current.FirstDay, this._selectedMetric), PreviousColor)
                {
                    IsStrokeDashed = true, ShowAreaFill = false
                }
            ])
            : new LineChartData([currentLine]);
    }

    private static LineChartPoint[] MetricPoints(AnalyticsPeriod period, DateTimeOffset first, int metric)
    {
        return period.Daily.Select((total, index) => new LineChartPoint(
            first.AddDays(index), metric switch
            {
                1 => total.EngagementRate * 100,
                2 => total.Purchases,
                3 => total.PurchaseRate * 100,
                _ => total.Sessions,
            })).ToArray();
    }

    private string FormatComparison(AnalyticsPeriod current)
    {
        var range = FormatRange(current.FirstDay, current.FirstDay.AddDays(current.Daily.Length - 1));
        return this._previous is { } earlier
            ? $"{range} compared with {FormatRange(earlier.FirstDay, current.FirstDay.AddDays(-1))} (UTC)"
            : $"{range} (UTC)";
    }

    private static string FormatRange(DateTimeOffset first, DateTimeOffset last)
    {
        return first == last ? $"{last:d MMM yyyy}" :
            first.Year == last.Year ? $"{first:d MMM} - {last:d MMM yyyy}" : $"{first:d MMM yyyy} - {last:d MMM yyyy}";
    }

    private void ShowAcquisition(AnalyticsPeriod current)
    {
        this.AcquisitionPeriod.Text = this.FormatComparison(current);
        var selectedChannel = this.AnalyticsChannelBox.SelectedIndex - 1;
        var channels = selectedChannel < 0
            ? Enumerable.Range(0, Channels.Length).OrderByDescending(index => current.ByChannel[index].Sessions).ToArray()
            : [selectedChannel];
        var labels = channels.Select(index => Channels[index]).ToArray();
        var currentBars
            = new BarChartSeries("Selected period", Values(current), new SolidColorBrush(SamplePalette.Blue)) { Key = "current" };
        this.SessionChannels.Data = this._previous is { } previous
            ? new BarChartData(
                labels, [
                    currentBars,
                    new BarChartSeries("Previous period", Values(previous), new SolidColorBrush(PreviousColor)) { Key = "previous" }
                ], labels)
            : new BarChartData(labels, [currentBars], labels);
        this.UpdateChannelsTable(current);

        double?[] Values(AnalyticsPeriod period)
        {
            return channels.Select(index => (double?)period.ByChannel[index].Sessions).ToArray();
        }
    }

    private void UpdateChannelsTable(AnalyticsPeriod current)
    {
        var sort = this.ChannelSortBox.SelectedIndex;
        var selected = this.AnalyticsChannelBox.SelectedIndex - 1;
        this.ChannelsTable.ItemsSource = Enumerable.Range(0, Channels.Length).Where(index => selected < 0 || index == selected)
            .OrderByDescending(index => sort switch
            {
                1 => current.ByChannel[index].Purchases,
                2 => current.ByChannel[index].PurchaseRate,
                _ => current.ByChannel[index].Sessions,
            }).Select(index => ToTrafficRow(Channels[index], current.ByChannel[index], index, showPurchases: true)).ToArray();
    }

    private void OnChannelSortChanged(object sender, SelectionChangedEventArgs args)
    {
        if (this._ready && this.SelectedReport == 1 && this._current is { } current)
        {
            this.UpdateChannelsTable(current);
        }
    }

    private void OnChannelInvoked(object sender, ItemClickEventArgs args)
    {
        if (args.ClickedItem is AnalyticsReportRow row)
        {
            this.AnalyticsChannelBox.SelectedIndex = row.Key + 1;
        }
    }

    private void OnLandingSearchChanged(object sender, TextChangedEventArgs args)
    {
        if (this._ready && this.SelectedReport == 2 && this._current is { } current)
        {
            this.UpdateLandingTable(current);
        }
    }

    private void UpdateLandingTable(AnalyticsPeriod current)
    {
        var term = this.LandingSearch.Text.Trim();
        var rows = LandingPages.Select((page, index) => (Page: page, Totals: current.ByLandingPage[index]))
            .Where(item => item.Totals.Sessions > 0 && item.Page.Contains(term, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.Totals.Sessions).Select(item => ToTrafficRow(item.Page, item.Totals, -1, showPurchases: false))
            .ToArray();
        this.LandingTable.ItemsSource = rows;
        this.NoLandingPages.Visibility = rows.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static AnalyticsReportRow ToTrafficRow(string name, AnalyticsTotals totals, int key, bool showPurchases)
    {
        return new(
            name, $"{totals.Sessions:N0}", $"{(showPurchases ? totals.Purchases : totals.Engaged):N0}",
            $"{(showPurchases ? totals.PurchaseRate : totals.EngagementRate):P1}", key);
    }

    private static HeatmapChartData CreateActivityData(AnalyticsPeriod period)
    {
        var culture = CultureInfo.CurrentCulture;
        var rows = Enumerable.Range(1, 7).Select(day => culture.DateTimeFormat.GetAbbreviatedDayName((DayOfWeek)(day % 7))).ToArray();
        var columns = new string[period.SessionsByHourBlock.GetLength(1)];
        var cells = new HeatmapChartCell?[rows.Length, columns.Length];

        for (var column = 0; column < columns.Length; column++)
        {
            var hour = column * 3;
            columns[column] = hour.ToString("00", CultureInfo.InvariantCulture);
            for (var row = 0; row < rows.Length; row++)
            {
                var days = period.DaysByWeekday[row];
                if (days > 0)
                {
                    var average = (double)period.SessionsByHourBlock[row, column] / days;
                    cells[row, column] = new HeatmapChartCell(average, $"{rows[row]} {hour:00}:00-{hour + 3:00}:00 UTC");
                }
            }
        }

        return new HeatmapChartData(cells, rows, columns);
    }

    private void ShowCheckout(AnalyticsPeriod current)
    {
        var selectedDevice = this.AnalyticsDeviceBox.SelectedIndex - 1;
        var devices = selectedDevice < 0 ? Enumerable.Range(0, Devices.Length).ToArray() : [selectedDevice];
        var cohorts = devices.Select(index => new FunnelChartSeries(
            Devices[index], current.CheckoutByDevice[index], new SolidColorBrush(DeviceColors[index]))).ToArray();
        var funnel = FunnelChartData.FromSeries(CheckoutSteps, cohorts);
        this.CheckoutJourney.Data = funnel;

        var rows = new AnalyticsReportRow[funnel.Stages.Count];
        rows[0] = new AnalyticsReportRow(funnel.Stages[0].Label, $"{funnel.Stages[0].Value:N0}", "-", "-", 0);
        var largestDrop = 0d;
        this.DropOffSummary.Text = "No sessions dropped off in the selected range.";
        for (var step = 1; step < rows.Length; step++)
        {
            var stage = funnel.Stages[step];
            var lastStage = funnel.Stages[step - 1];
            var lost = lastStage.Value - stage.Value;
            var drop = lastStage.Value > 0 ? lost / lastStage.Value : 0;
            rows[step] = new AnalyticsReportRow(
                stage.Label, $"{stage.Value:N0}", $"{lost:N0}", lastStage.Value > 0 ? FormatShare(stage.Value, lastStage.Value) : "-",
                step);
            if (drop > largestDrop)
            {
                largestDrop = drop;
                this.DropOffSummary.Text
                    = $"{drop:P1} of sessions dropped off between {lastStage.Label} and {stage.Label} ({lost:N0} sessions).";
            }
        }

        this.CheckoutTable.ItemsSource = rows;
    }

    private void UpdateColorScale()
    {
        this.VisitorHours.ColorScale = HeatmapColorScale.FromPalette(ActivityRamp, ActivityStops, this.VisitorHours.ActualTheme);
    }

    private void OnWorkspaceSizeChanged(object sender, SizeChangedEventArgs args)
    {
        this.UpdateLayout(args.NewSize.Width);
    }

    private void UpdateLayout(double width)
    {
        var compact = width < 690;
        if (!this._ready || width <= 0 || compact == this._compact)
        {
            return;
        }

        this._compact = compact;
        ArrangeColumns(this.MetricGrid, compact ? [1, 1] : [1, 1, 1, 1]);
        ArrangeColumns(this.Filters, compact ? [1.2, 1] : [1.25, 1, 1]);
        Grid.SetColumn(this.DeviceSurface, compact ? 0 : 1);
        Grid.SetRow(this.DeviceSurface, compact ? 1 : 0);
        this.OverviewCharts.ColumnSpacing = compact ? 0 : 14;
        this.OverviewCharts.ColumnDefinitions[1].Width = compact ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
    }

    private static void ArrangeColumns(Grid grid, double[] widths)
    {
        grid.ColumnDefinitions.Clear();
        foreach (var width in widths)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(width, GridUnitType.Star) });
        }

        for (var index = 0; index < grid.Children.Count; index++)
        {
            Grid.SetColumn((FrameworkElement)grid.Children[index], index % widths.Length);
            Grid.SetRow((FrameworkElement)grid.Children[index], index / widths.Length);
        }
    }
}

public sealed record AnalyticsReportRow(string Name, string Count, string Detail, string Rate, int Key);
