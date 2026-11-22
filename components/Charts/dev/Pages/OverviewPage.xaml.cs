// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using JPSoftworks.DesktopToolkit.Controls.Demo.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Pages;

public sealed partial class OverviewPage : Page
{
    public OverviewPage()
    {
        this.InitializeComponent();
        this.PreviewGraph.AddSeries("CPU", SamplePalette.Blue, SamplePalette.BlueFill);
        this.PreviewGraph.AddSeries("Memory", SamplePalette.Purple, SamplePalette.PurpleFill);
        var now = DateTimeOffset.UtcNow;
        for (var second = -60; second <= 0; second++)
        {
            this.PreviewGraph.AppendSample(
                now.AddSeconds(second), 44 + 22 * Math.Sin(second * .19) + 9 * Math.Sin(second * .57), 68 + 7 * Math.Sin(second * .11));
        }

        this.PreviewGraph.IsPaused = true;
    }

    private void OnLiveClick(object sender, RoutedEventArgs e)
    {
        this.Frame.Navigate(typeof(LiveAreaGraphPage));
    }

    private void OnLineChartClick(object sender, RoutedEventArgs e)
    {
        this.Frame.Navigate(typeof(LineChartPage));
    }

    private void OnScatterChartClick(object sender, RoutedEventArgs e)
    {
        this.Frame.Navigate(typeof(ScatterChartPage));
    }

    private void OnSpiderClick(object sender, RoutedEventArgs e)
    {
        this.Frame.Navigate(typeof(SpiderChartPage));
    }

    private void OnHeatmapClick(object sender, RoutedEventArgs e)
    {
        this.Frame.Navigate(typeof(HeatmapChartPage));
    }

    private void OnSankeyClick(object sender, RoutedEventArgs e)
    {
        this.Frame.Navigate(typeof(SankeyChartPage));
    }

    private void OnFunnelClick(object sender, RoutedEventArgs e)
    {
        this.Frame.Navigate(typeof(FunnelChartPage));
    }

    private void OnTreemapClick(object sender, RoutedEventArgs e)
    {
        this.Frame.Navigate(typeof(TreemapChartPage));
    }

    private void OnIcicleClick(object sender, RoutedEventArgs e)
    {
        this.Frame.Navigate(typeof(IcicleChartPage));
    }

    private void OnSunburstClick(object sender, RoutedEventArgs e)
    {
        this.Frame.Navigate(typeof(SunburstChartPage));
    }

    private void OnBarChartClick(object sender, RoutedEventArgs e)
    {
        this.Frame.Navigate(typeof(BarChartPage));
    }

    private void OnBarClick(object sender, RoutedEventArgs e)
    {
        this.Frame.Navigate(typeof(VerticalUsageBarPage));
    }

    private void OnGaugeClick(object sender, RoutedEventArgs e)
    {
        this.Frame.Navigate(typeof(RadialGaugePage));
    }

    private void OnDoughnutClick(object sender, RoutedEventArgs e)
    {
        this.Frame.Navigate(typeof(DoughnutGraphPage));
    }

    private void OnLegendsClick(object sender, RoutedEventArgs e)
    {
        this.Frame.Navigate(typeof(ChartLegendsPage));
    }

    private void OnDashboardWidgetsClick(object sender, RoutedEventArgs e)
    {
        this.Frame.Navigate(typeof(DashboardWidgetsPage));
    }

    private void OnMonitorClick(object sender, RoutedEventArgs e)
    {
        this.Frame.Navigate(typeof(SystemMonitorPage));
    }

    private void OnMemoryAnalysisClick(object sender, RoutedEventArgs e)
    {
        this.Frame.Navigate(typeof(MemoryAnalysisPage));
    }

    private void OnSalesPerformanceClick(object sender, RoutedEventArgs e)
    {
        this.Frame.Navigate(typeof(SalesPerformancePage));
    }

    private void OnWebAnalyticsClick(object sender, RoutedEventArgs e)
    {
        this.Frame.Navigate(typeof(WebAnalyticsPage));
    }
}
