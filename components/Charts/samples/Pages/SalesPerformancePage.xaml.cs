// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Globalization;
using JPSoftworks.DesktopToolkit.Charts;
using JPSoftworks.DesktopToolkit.Controls.Demo.Services;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Pages;

public sealed partial class SalesPerformancePage : Page
{
    private static readonly ChartColorSet ReturnRamp = ChartPalette.CreateRamp(Color.FromArgb(255, 192, 68, 42), 5);
    private static readonly double[] ReturnStops = [0, 3, 6, 9, 12];
    private readonly MonthlySales[] _data = RetailSalesSample.Create();
    private bool _ready;

    public SalesPerformancePage()
    {
        this.InitializeComponent();
        ChartPalette.SetColors(this.RevenueHistory, ChartPalette.Soft);
        this.RevenueHistory.XAxis = new ChartAxisOptions(tickLabelFormat: "MMM d");
        ChartPalette.SetColors(this.RegionalVariance, ChartPalette.Earth);
        ChartContextMenu.SetProvider(this.RevenueHistory, ChartDetailsMenuProvider.Instance);
        ChartContextMenu.SetProvider(this.RegionalVariance, ChartDetailsMenuProvider.Instance);
        ChartContextMenu.SetProvider(this.ReturnRates, ChartDetailsMenuProvider.Instance);
        this.ReturnRates.ActualThemeChanged += (_, _) => this.UpdateLegendScale();
        this.UpdateLegendScale();
        this.RevenueExample.Xaml = """
                                   <charts:LineChart Data="{x:Bind RevenueData, Mode=OneWay}"
                                       XValueFormat="MMM" ValueFormat="0.#' k USD'" />
                                   """;
        this.RevenueExample.CSharp = """
                                     RevenueHistory.XAxis = new ChartAxisOptions(tickLabelFormat: "MMM d");
                                     new LineChartData(
                                     [
                                         new LineChartSeries("Net revenue", revenuePoints),
                                         new LineChartSeries("Plan", targetPoints) { IsStrokeDashed = true },
                                     ]);
                                     """;
        this.VarianceExample.Xaml = """
                                    <charts:BarChart Data="{x:Bind VarianceData, Mode=OneWay}" Mode="Stacked" />
                                    """;
        this.ReturnsExample.Xaml = """
                                   <charts:HeatmapChart Data="{x:Bind ReturnsData, Mode=OneWay}" ValueFormat="0.#'%'" />
                                   """;
        this._ready = true;
        this.ApplyFilters();
    }

    private void OnFilterChanged(object sender, SelectionChangedEventArgs args)
    {
        if (this._ready)
        {
            this.ApplyFilters();
        }
    }

    private void ApplyFilters()
    {
        var region = this.RegionBox.SelectedIndex - 1;
        var quarter = this.PeriodBox.SelectedIndex - 1;
        var months = Enumerable.Range(quarter < 0 ? 0 : quarter * 3, quarter < 0 ? 12 : 3).ToArray();
        var regions = region < 0 ? Enumerable.Range(0, RetailSalesSample.Regions.Count).ToArray() : [region];
        var selected = this._data.Where(item => months.Contains(item.Month) && regions.Contains(item.Region)).ToArray();
        var byMonth = selected.ToLookup(item => item.Month);
        var byRegionAndMonth = selected.ToLookup(item => (item.Region, item.Month));
        var byChannelAndMonth = selected.ToLookup(item => (item.Channel, item.Month));
        var columns = months.Select(month => RetailSalesSample.Month(month).ToString("MMM", CultureInfo.CurrentCulture)).ToArray();

        this.RevenueHistory.Data = new LineChartData(
        [
            new LineChartSeries(
                "Net revenue",
                months.Select(month => new LineChartPoint(RetailSalesSample.Month(month), byMonth[month].Sum(item => item.NetRevenue)))
                    .ToArray()),
            new LineChartSeries(
                "Plan",
                months.Select(month => new LineChartPoint(RetailSalesSample.Month(month), byMonth[month].Sum(item => item.Plan))).ToArray())
            {
                IsStrokeDashed = true
            },
        ]);

        this.RegionalVariance.Data = new BarChartData(
            columns,
            regions.Select(market => new BarChartSeries(
                RetailSalesSample.Regions[market],
                months.Select(month => (double?)byRegionAndMonth[(market, month)].Sum(item => item.NetRevenue - item.Plan)).ToArray())
            {
                Key = RetailSalesSample.Regions[market]
            }).ToArray(), months.Select(month => month.ToString(CultureInfo.InvariantCulture)).ToArray());

        var returns = new double?[RetailSalesSample.Channels.Count, months.Length];
        for (var channel = 0; channel < returns.GetLength(0); channel++)
        {
            for (var column = 0; column < months.Length; column++)
            {
                var bucket = byChannelAndMonth[(channel, months[column])];
                var count = bucket.Sum(item => item.Orders);
                returns[channel, column] = count > 0 ? 100d * bucket.Sum(item => item.ReturnedOrders) / count : null;
            }
        }

        this.ReturnRates.Data = new HeatmapChartData(returns, RetailSalesSample.Channels.ToArray(), columns);

        var net = selected.Sum(item => item.NetRevenue);
        var plan = selected.Sum(item => item.Plan);
        var orders = selected.Sum(item => item.Orders);
        var rate = 100d * selected.Sum(item => item.ReturnedOrders) / orders;
        this.SalesSummary.Text = $"{net:N0} k USD net revenue   |   {(net - plan):+0;-0;0} k USD vs plan   |   {rate:0.0}% orders returned";
    }

    private void UpdateLegendScale()
    {
        this.ReturnRates.ColorScale = HeatmapColorScale.FromPalette(ReturnRamp, ReturnStops, this.ReturnRates.ActualTheme);
    }
}
