// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Globalization;
using JPSoftworks.DesktopToolkit.Charts;
using JPSoftworks.DesktopToolkit.Controls.Demo.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Pages;

public sealed partial class HeatmapChartPage : Page
{
    private static readonly double[] ContributionStops = [0, 1, 3, 5, 8];
    private static readonly ChartColorSet LatencyRamp = ChartPalette.CreateRamp(Color.FromArgb(255, 209, 103, 40), 4);

    public HeatmapChartData CalendarData { get; }

    public string CalendarRange { get; }

    public HeatmapChartData LatencyData { get; } = new(
        new double?[,] { { 42, 38, 56, 61, 40, 26 }, { 48, 51, 72, 47, 39, 31 }, { 21, 25, 42, 77, 64, 39 }, { 39, 67, 73, 51, 48, null } },
        ["Americas", "Europe", "Asia Pacific", "Oceania"], ["00:00", "04:00", "08:00", "12:00", "16:00", "20:00"]);

    public HeatmapChartPage()
    {
        var end = DateTime.Today;
        var start = end.AddDays(-364);
        this.CalendarData = CreateCalendar(start, end);
        this.CalendarRange = $"{start:d} to {end:d}";
        this.InitializeComponent();
        this.UpdateCalendarScale();
        this.Calendar.ActualThemeChanged += (_, _) => this.UpdateCalendarScale();
        this.Latency.ActualThemeChanged += (_, _) => this.UpdateLatencyScale();
        ChartContextMenu.SetProvider(this.Calendar, ChartDetailsMenuProvider.Instance);
        ChartContextMenu.SetProvider(this.Latency, ChartDetailsMenuProvider.Instance);
        ChartContextMenu.SetProvider(this.LatencyTiles, ChartDetailsMenuProvider.Instance);
        this.CalendarExample.Xaml = """
                                    <charts:HeatmapChart Height="180" Data="{x:Bind CalendarData}" ColorScale="{x:Bind ContributionScale}"
                                        KeepCellsSquare="True" ValueFormat="0" />
                                    """;
        this.CalendarExample.CSharp = """
                                      public HeatmapColorScale ContributionScale { get; } =
                                          HeatmapColorScale.FromPalette(ChartPalette.ActivityGreen, [0, 1, 3, 5, 8], isStepped: true);

                                      public HeatmapChartData CalendarData { get; } = CreateCalendar();

                                      private static HeatmapChartData CreateCalendar()
                                      {
                                          var end = DateTime.UtcNow.Date;
                                          var start = end.AddDays(-364);
                                          var firstSunday = start.AddDays(-(int)start.DayOfWeek);
                                          var weeks = (end - firstSunday).Days / 7 + 1;
                                          var cells = new HeatmapChartCell?[7, weeks];
                                          var months = new string?[weeks];
                                          var random = new Random(415);
                                          for (var day = start; day <= end; day = day.AddDays(1))
                                          {
                                              var week = (day - firstSunday).Days / 7;
                                              cells[(int)day.DayOfWeek, week] = new(random.Next(9),
                                                  Timestamp: new DateTimeOffset(day));
                                              if (day == start || day.Day == 1)
                                              {
                                                  months[week] = day.ToString("MMM");
                                              }
                                          }

                                          return new HeatmapChartData(cells, [null, "Mon", null, "Wed", null, "Fri", null], months);
                                      }
                                      """;
        this.MatrixExample.Xaml = """
                                  <charts:HeatmapChart Height="230" Data="{x:Bind LatencyData}" ColorScale="{x:Bind LatencyBands}"
                                      CellSpacing="2" CellCornerRadius="2" ValueFormat="0' ms'" />
                                  """;
        this.MatrixExample.CSharp = """
                                    public HeatmapChartData LatencyData { get; } = new(
                                        new double?[,] { { 42, 38, 56 }, { 21, 38, null } },
                                        ["Americas", "Europe"], ["00:00", "04:00", "08:00"]);

                                    public HeatmapColorScale LatencyBands { get; } = HeatmapColorScale.FromPalette(
                                        ChartPalette.CreateRamp(Windows.UI.Color.FromArgb(255, 209, 103, 40), 4),
                                        [20, 40, 60, 80], isStepped: true);
                                    """;
        this.TilesExample.Xaml = """
                                 <charts:HeatmapChart Height="154" Data="{x:Bind LatencyData}"
                                     charts:ChartAxisAppearance.Presentation="Hidden" CellSpacing="5" CellCornerRadius="6"
                                     ValueFormat="0' ms'" AutomationProperties.Name="Compact heatmap of request latency by region and period" />
                                 """;
        this.TilesExample.CSharp = """
                                   public HeatmapChartData LatencyData { get; } = new(
                                       new double?[,] { { 42, 38, 56 }, { 21, 38, null } },
                                       ["Americas", "Europe"], ["00:00", "04:00", "08:00"]);
                                   """;
    }

    private void UpdateCalendarScale()
    {
        this.Calendar.ColorScale = HeatmapColorScale.FromPalette(
            ChartPalette.ActivityGreen, ContributionStops, this.Calendar.ActualTheme, isStepped: true);
    }

    private void OnScaleChanged(object sender, SelectionChangedEventArgs args)
    {
        this.UpdateLatencyScale();
    }

    private void UpdateLatencyScale()
    {
        if (this.Latency is not null && this.ScaleBox is not null)
        {
            this.Latency.ColorScale = this.ScaleBox.SelectedIndex == 1
                ? HeatmapColorScale.FromPalette(LatencyRamp, [20, 40, 60, 80], this.Latency.ActualTheme, isStepped: true)
                : null;
        }
    }

    private static HeatmapChartData CreateCalendar(DateTime start, DateTime end)
    {
        var culture = CultureInfo.CurrentCulture;
        var firstSunday = start.AddDays(-(int)start.DayOfWeek);
        var weeks = (end - firstSunday).Days / 7 + 1;
        var cells = new HeatmapChartCell?[7, weeks];
        var months = new string?[weeks];
        var random = new Random(415);
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            var row = (int)day.DayOfWeek;
            var week = (day - firstSunday).Days / 7;
            var weekday = row is >= 1 and <= 5;
            var activity = 0.60 + 0.20 * Math.Sin(week * 0.43) + 0.12 * Math.Cos(week * 1.13);
            var count = random.NextDouble() < (weekday ? activity : activity * 0.38)
                ? 1 + (int)(Math.Pow(random.NextDouble(), 1.8) * (weekday ? 12 : 8))
                : 0;
            cells[row, week] = new HeatmapChartCell(count, Timestamp: new DateTimeOffset(day.Ticks, TimeSpan.Zero));
            if (day == start || day.Day == 1)
            {
                months[week] = day.ToString("MMM", culture);
            }
        }

        string?[] days =
        [
            null, culture.DateTimeFormat.GetAbbreviatedDayName(DayOfWeek.Monday), null,
            culture.DateTimeFormat.GetAbbreviatedDayName(DayOfWeek.Wednesday), null,
            culture.DateTimeFormat.GetAbbreviatedDayName(DayOfWeek.Friday), null
        ];
        return new HeatmapChartData(cells, days, months);
    }
}
