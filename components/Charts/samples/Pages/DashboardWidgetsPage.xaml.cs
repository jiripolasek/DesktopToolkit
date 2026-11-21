// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Globalization;
using JPSoftworks.DesktopToolkit.Charts;
using JPSoftworks.DesktopToolkit.Controls.Demo.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Pages;

public sealed partial class DashboardWidgetsPage : Page
{
    private const double InUseMemory = 23.6;
    private const double ModifiedMemory = 0.7;
    private const double StandbyMemory = 5.1;
    private const double FreeMemory = 2.6;
    private const double UsedMemory = InUseMemory + ModifiedMemory;
    private const double InstalledMemory = UsedMemory + StandbyMemory + FreeMemory;
    private const double LatestLatency = 142;
    private const double LatestRequests = 1240;

    private ChartAxisOptions UnlabeledAxis { get; } = new(showLabels: false);

    private ChartAxisOptions ElapsedAxis { get; } = new(minimum: -60, maximum: 0);

    private ChartAxisOptions MemoryUsageAxis { get; } = new(minimum: 0, maximum: InstalledMemory);

    private ChartAxisOptions SubtleTimeAxis { get; } = new(minimum: -60, maximum: 0) { LabelOpacity = 0.62 };

    private BarChartData MemoryComposition { get; } = new(
        ["Installed memory"],
        [new("In use", [InUseMemory]), new("Modified", [ModifiedMemory]), new("Standby", [StandbyMemory]), new("Free", [FreeMemory])]);

    private LineChartData MemorySamples { get; } = new(
    [
        new(
            "Used memory", [
                new(-60, 19.6), new(-55, 19.8), new(-50, 19.4), new(-45, 20.6), new(-40, 20.1), new(-35, 22.4),
                new(-30, 23.0), new(-25, 22.6), new(-20, 23.4), new(-15, 23.4), new(-10, 24.0), new(-5, 24.2), new(0, UsedMemory),
            ])
    ]);

    private LineChartData LatencySamples { get; } = new(
    [
        new(
            "95th percentile", [
                new(-60, 164), new(-55, 151), new(-50, 169), new(-45, 157), new(-40, 143), new(-35, 187),
                new(-30, 192), new(-25, 170), new(-20, 161), new(-15, 149), new(-10, 154), new(-5, 139), new(0, LatestLatency),
            ])
    ]);

    private LineChartData RequestsSamples { get; } = new(
    [
        new(
            "Requests per minute", [
                new(-60, 540), new(-55, 612), new(-50, 570), new(-45, 820), new(-40, 745), new(-35, 910),
                new(-30, 865), new(-25, 810), new(-20, 1050), new(-15, 1155), new(-10, 980), new(-5, 1085), new(0, LatestRequests),
            ])
    ]);

    private BarChartData WeeklyJobCounts { get; } = new(
        ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"],
        [new("Completed jobs", [76, 118, 95, 143, 122, 183, 137])]);

    private string CompletedJobsLabel =>
        this.WeeklyJobCounts.Series[0].Values.Sum()?.ToString("N0", CultureInfo.CurrentCulture) ?? string.Empty;

    private string UsedMemoryLabel => UsedMemory.ToString("0.#' GB'", CultureInfo.CurrentCulture);

    private string InstalledMemoryLabel => InstalledMemory.ToString("0.#' GB installed'", CultureInfo.CurrentCulture);

    private string LatencyLabel => LatestLatency.ToString("0' ms'", CultureInfo.CurrentCulture);

    private string RequestsLabel => LatestRequests.ToString("N0", CultureInfo.CurrentCulture);

    public DashboardWidgetsPage()
    {
        this.InitializeComponent();
        this.WidgetExample.Xaml = """
                                  <charts:BarChart Height="48" MinHeight="0" Padding="0" BorderThickness="0"
                                      Background="Transparent" Orientation="Horizontal" Mode="Stacked100"
                                      InspectionMode="Category" SegmentGap="3" SegmentCornerRadius="4"
                                      charts:ChartAxisAppearance.Presentation="Hidden"
                                      Data="{x:Bind MemoryComposition}"
                                      ValueFormat="0.#' GB'" />
                                  <charts:LineChart Height="74" MinHeight="0" Padding="0" BorderThickness="0"
                                      Background="Transparent" ShowLegend="False" ShowAreaFill="True"
                                      charts:ChartAxisAppearance.Presentation="Subtle"
                                      XAxis="{x:Bind SubtleTimeAxis}" YAxis="{x:Bind UnlabeledAxis}"
                                      Data="{x:Bind LatencySamples}" ValueFormat="0' ms'" />
                                  <charts:LineChart Height="78" MinHeight="0" Padding="0" BorderThickness="0" CornerRadius="0,0,11,11"
                                      Background="Transparent" ShowLegend="False" ShowAreaFill="True"
                                      charts:ChartAxisAppearance.Presentation="Hidden" XAxis="{x:Bind ElapsedAxis}"
                                      Data="{x:Bind RequestsSamples}" />
                                  <charts:BarChart Height="80" MinHeight="0" Padding="0" BorderThickness="0"
                                      CornerRadius="0,0,11,11" Background="Transparent" BarSpacing="2" ShowLegend="False"
                                      charts:ChartAxisAppearance.Presentation="Hidden" Data="{x:Bind WeeklyJobCounts}"
                                      ValueFormat="N0" AutomationProperties.Name="Completed jobs by day over the last seven days" />
                                  """;
        this.WidgetExample.CSharp = """
                                    private ChartAxisOptions UnlabeledAxis { get; } = new(showLabels: false);
                                    private ChartAxisOptions ElapsedAxis { get; } = new(minimum: -60, maximum: 0);
                                    private ChartAxisOptions SubtleTimeAxis { get; } = new(minimum: -60, maximum: 0) { LabelOpacity = 0.62 };
                                    private BarChartData MemoryComposition { get; } = new(["Installed memory"],
                                        [new("In use", [23.6]), new("Modified", [0.7]), new("Standby", [5.1]), new("Free", [2.6])]);

                                    private BarChartData WeeklyJobCounts { get; } = new(
                                        ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"],
                                        [new("Completed jobs", [76, 118, 95, 143, 122, 183, 137])]);
                                    """;
        ChartContextMenu.SetProvider(this.MemoryHistory, ChartDetailsMenuProvider.Instance);
        ChartContextMenu.SetProvider(this.MemorySplit, ChartDetailsMenuProvider.Instance);
        ChartContextMenu.SetProvider(this.LatencyHistory, ChartDetailsMenuProvider.Instance);
        ChartContextMenu.SetProvider(this.RequestsHistory, ChartDetailsMenuProvider.Instance);
        ChartContextMenu.SetProvider(this.WeeklyJobs, ChartDetailsMenuProvider.Instance);
        ChartPalette.SetColors(this.RequestsHistory, ChartPalette.Earth);
    }

    private void OnTilesSizeChanged(object sender, SizeChangedEventArgs args)
    {
        var wide = args.NewSize.Width >= 700;
        this.Tiles.ColumnSpacing = wide ? 16 : 0;
        this.Tiles.RowSpacing = wide ? 0 : 16;
        this.LatencyColumn.Width = new GridLength(wide ? 300 : 0);
        Grid.SetColumn(this.SecondaryTiles, wide ? 1 : 0);
        Grid.SetRow(this.SecondaryTiles, wide ? 0 : 1);
    }
}
