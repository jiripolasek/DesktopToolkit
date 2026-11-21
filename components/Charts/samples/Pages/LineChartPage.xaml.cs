// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using JPSoftworks.DesktopToolkit.Charts;
using JPSoftworks.DesktopToolkit.Controls.Demo.Controls;
using JPSoftworks.DesktopToolkit.Controls.Demo.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Pages;

public sealed partial class LineChartPage : Page
{
    private static readonly DateTimeOffset TemperatureStart = new(2026, 1, 12, 9, 0, 0, TimeSpan.Zero);

    public LineChartData NumericData { get; } = new(
    [
        new LineChartSeries("Before tuning", [new(0, 32), new(10, 39), new(40, null), new(70, 92), new(100, 110)]),
        new LineChartSeries("After tuning", [new(0, 21), new(20, 29), new(50, 41), new(80, 64), new(100, 76)]),
    ]);

    public LineChartData TemperatureData { get; } = CreateTemperatureData();

    private ChartMilestones TemperatureMilestones { get; } = new(
        new ChartMilestone(TemperatureStart.AddMinutes(15), "Firmware rollout"),
        new ChartMilestone(TemperatureStart.AddMinutes(40), "Fan curve adjusted"));

    private ChartAxisOptions LatencyAxis { get; } = new() { ReferenceLines = (ChartReferenceLine[])[new(80, "80 ms budget")], };

    public LineChartData RequestData { get; } = CreateRequestData();

    public LineChartPage()
    {
        this.InitializeComponent();
        ChartContextMenu.SetProvider(this.NumericChart, ChartDetailsMenuProvider.Instance);
        ChartContextMenu.SetProvider(this.CompactLatencyChart, ChartDetailsMenuProvider.Instance);
        ChartContextMenu.SetProvider(this.TemperatureChart, ChartDetailsMenuProvider.Instance);
        this.NumericExample.Xaml = """
                                   <charts:LineChart Height="250" Data="{x:Bind NumericData}" YAxis="{x:Bind LatencyAxis}"
                                       IsViewportInteractionEnabled="True" IsSelectionEnabled="True"
                                       XValueFormat="0' r/s'" ValueFormat="0' ms'"
                                       AutomationProperties.Name="Latency by request rate" />
                                   """;
        this.NumericExample.CSharp = """
                                     private ChartAxisOptions LatencyAxis { get; } = new()
                                     {
                                         ReferenceLines = (ChartReferenceLine[])[new(80, "80 ms budget")],
                                     };

                                     public LineChartData NumericData { get; } = new(
                                     [
                                         new LineChartSeries("Before tuning",
                                             [new(0, 32), new(10, 39), new(40, null), new(70, 92), new(100, 110)]),
                                         new LineChartSeries("After tuning",
                                             [new(0, 21), new(20, 29), new(50, 41), new(80, 64), new(100, 76)]),
                                     ]);
                                     """;
        this.CompactLatencyExample.Xaml = ChartStylingGuide.Get(nameof(LineChart))?.Xaml
                                          + "\n\n"
                                          + """
                                            <charts:LineChart Height="140" Data="{x:Bind NumericData}" YAxis="{x:Bind LatencyAxis}"
                                                Style="{StaticResource CompactTrend}"
                                                XValueFormat="0' r/s'" ValueFormat="0' ms'"
                                                AutomationProperties.Name="Compact latency comparison by request rate" />
                                            """;
        this.CompactLatencyExample.CSharp = this.NumericExample.CSharp;
        this.TimeExample.Xaml = """
                                <StackPanel Spacing="12">
                                    <charts:LineChart x:Name="TemperatureChart" Height="250" Data="{x:Bind TemperatureData}"
                                        Milestones="{x:Bind TemperatureMilestones}"
                                        charts:ChartLegend.ValueMode="Value" charts:ChartLegend.HighlightOnHover="True"
                                        XValueFormat="HH:mm" ValueFormat="0.#' C'" ShowAreaFill="True"
                                        AutomationProperties.Name="Sensor temperatures over one hour in UTC" />
                                    <ToggleSwitch Header="Interpolation" OffContent="Straight" OnContent="Monotone"
                                        Toggled="OnInterpolationToggled" />
                                </StackPanel>
                                """;
        this.TimeExample.CSharp = """
                                  private static readonly DateTimeOffset TemperatureStart = new(2026, 1, 12, 9, 0, 0, TimeSpan.Zero);

                                  public LineChartData TemperatureData { get; } = CreateTemperatureData();

                                  private ChartMilestones TemperatureMilestones { get; } = new(
                                      new ChartMilestone(TemperatureStart.AddMinutes(15), "Firmware rollout"),
                                      new ChartMilestone(TemperatureStart.AddMinutes(40), "Fan curve adjusted"));

                                  private static LineChartData CreateTemperatureData()
                                  {
                                      var start = TemperatureStart;
                                      return new LineChartData(
                                      [
                                          new LineChartSeries("Intake",
                                              [new(start, 21), new(start.AddMinutes(10), 23),
                                               new(start.AddMinutes(40), 21.5), new(start.AddHours(1), 22)]),
                                          new LineChartSeries("Exhaust",
                                              [new(start.AddMinutes(5), 24), new(start.AddMinutes(11), 24.5),
                                               new(start.AddMinutes(16), 26), new(start.AddMinutes(20), null),
                                               new(start.AddMinutes(45), 27), new(start.AddMinutes(50), 28.2),
                                               new(start.AddMinutes(55), 26)]),
                                      ]);
                                  }

                                  private void OnInterpolationToggled(object sender, RoutedEventArgs e) =>
                                      TemperatureChart.Interpolation = ((ToggleSwitch)sender).IsOn
                                          ? LineChartInterpolation.Monotone
                                          : LineChartInterpolation.Linear;
                                  """;
        this.StackedExample.Xaml = """
                                   <charts:LineChart Height="250" Mode="StackedArea" Interpolation="Monotone"
                                       Data="{x:Bind RequestData}" XValueFormat="HH:mm" ValueFormat="0"
                                       AutomationProperties.Name="Stacked hourly requests by service in UTC" />
                                   """;
        this.StackedExample.CSharp = """
                                     public LineChartData RequestData { get; } = CreateRequestData();

                                     private static LineChartData CreateRequestData()
                                     {
                                         var start = new DateTimeOffset(2026, 1, 12, 8, 0, 0, TimeSpan.Zero);
                                         LineChartSeries Hours(string name, double?[] values) =>
                                             new(name, values.Select((value, hour) =>
                                                 new LineChartPoint(start.AddHours(hour), value)).ToArray());

                                         return new LineChartData(
                                         [
                                             Hours("Static assets", [28, 32, 30, 34, 36, 33, 39, 42, 38]),
                                             Hours("API", [13, 18, 20, null, 17, 22, 23, 19, 22]),
                                             Hours("Media", [8, 12, 10, 14, 12, 9, null, 14, 11]),
                                         ]);
                                     }
                                     """;
    }

    private ChartMilestones? GetTemperatureMilestones(bool show)
    {
        return show ? this.TemperatureMilestones : null;
    }

    private ChartAxisOptions? GetLatencyAxis(bool showBudget)
    {
        return showBudget ? this.LatencyAxis : null;
    }

    private string GetViewportSummary(LineChartViewport? viewport, LineChartViewport? selection)
    {
        return $"Visible {FormatRange(viewport ?? this.NumericData.Extent)}\nSelection {FormatRange(selection)}";
    }

    private static string FormatRange(LineChartViewport? range)
    {
        return range is { Minimum: { } minimum, Maximum: { } maximum } ? $"{minimum:0.#} to {maximum:0.#} r/s" : "None";
    }

    private void OnCustomReadoutsToggled(object sender, RoutedEventArgs args)
    {
        var enabled = ((ToggleSwitch)sender).IsOn;
        ChartLegend.SetItemTemplate(this.NumericChart, enabled ? (DataTemplate)this.Resources["LatencyLegendTemplate"] : null);
        ChartInspection.SetContentTemplate(this.NumericChart, enabled ? (DataTemplate)this.Resources["LatencyInspectorTemplate"] : null);
    }

    private static LineChartData CreateTemperatureData()
    {
        var start = TemperatureStart;
        return new LineChartData(
        [
            new LineChartSeries(
                "Intake", [
                    new(start, 21), new(start.AddMinutes(10), 23),
                    new(start.AddMinutes(40), 21.5), new(start.AddHours(1), 22)
                ]),
            new LineChartSeries(
                "Exhaust", [
                    new(start.AddMinutes(5), 24), new(start.AddMinutes(11), 24.5),
                    new(start.AddMinutes(16), 26), new(start.AddMinutes(20), null),
                    new(start.AddMinutes(45), 27), new(start.AddMinutes(50), 28.2),
                    new(start.AddMinutes(55), 26)
                ]),
        ]);
    }

    private void OnAreaFillClick(object sender, RoutedEventArgs e)
    {
        this.TemperatureChart.ShowAreaFill = ((CheckBox)sender).IsChecked == true;
    }

    private static LineChartData CreateRequestData()
    {
        var start = new DateTimeOffset(2026, 1, 12, 8, 0, 0, TimeSpan.Zero);

        LineChartSeries Hours(string name, double?[] values)
        {
            return new(name, values.Select((value, hour) => new LineChartPoint(start.AddHours(hour), value)).ToArray());
        }

        return new LineChartData(
        [
            Hours("Static assets", [28, 32, 30, 34, 36, 33, 39, 42, 38]),
            Hours("API", [13, 18, 20, null, 17, 22, 23, 19, 22]),
            Hours("Media", [8, 12, 10, 14, 12, 9, null, 14, 11]),
        ]);
    }

    private void OnInterpolationToggled(object sender, RoutedEventArgs e)
    {
        this.TemperatureChart.Interpolation = ((ToggleSwitch)sender).IsOn ? LineChartInterpolation.Monotone : LineChartInterpolation.Linear;
    }
}
