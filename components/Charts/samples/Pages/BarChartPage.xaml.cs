// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using JPSoftworks.DesktopToolkit.Charts;
using JPSoftworks.DesktopToolkit.Controls.Demo.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Pages;

public sealed partial class BarChartPage : Page
{
    public BarChartData CpuData { get; } = new(
        ["CPU 0", "CPU 1", "CPU 2", "CPU 3", "CPU 4", "CPU 5"], [
            new BarChartSeries("Before", [24, 81, 40, 58, 71, 32]),
            new BarChartSeries("After", [32, 93, 54, 75, 64, 41]),
        ]);

    private ChartAxisOptions CpuAxis { get; } = new() { ReferenceLines = (ChartReferenceLine[])[new(85, "85% threshold")] };

    private ChartAxisOptions PercentCpuAxis { get; } = new() { ReferenceLines = (ChartReferenceLine[])[new(0.85, "85% threshold")] };

    public BarChartPage()
    {
        this.InitializeComponent();
        ChartContextMenu.SetProvider(this.CpuChart, ChartDetailsMenuProvider.Instance);
        ChartContextMenu.SetProvider(this.CompactCpuChart, ChartDetailsMenuProvider.Instance);
        ChartContextMenu.SetProvider(this.PercentStackChart, ChartDetailsMenuProvider.Instance);
        this.StorageChart.Data = new BarChartData(
            ["Apps", "Documents", "Downloads", "Media"], [
                new BarChartSeries("Previous week", [6, 2, -7, 3]),
                new BarChartSeries("This week", [3, 8, -4, -5]),
            ]);
        this.SignedStackChart.Data = new BarChartData(
            ["January", "February", "March"], [
                new BarChartSeries("Retail", [40, 32, 56]),
                new BarChartSeries("Subscriptions", [20, 24, 18]),
                new BarChartSeries("Refunds", [-12, -8, -14]),
                new BarChartSeries("Credits", [-4, -6, -7]),
            ]);
        this.PercentStackChart.Data = new BarChartData(
            ["January", "February", "March"], [
                new BarChartSeries("Web", [30, 50, 10]),
                new BarChartSeries("Desktop", [10, 30, 50]),
                new BarChartSeries("Mobile", [10, 20, 20]),
            ]);

        this.CpuExample.Xaml = """
                               <charts:BarChart Height="245" Data="{x:Bind CpuData}" Maximum="100" ValueAxis="{x:Bind CpuAxis}"
                                   ValueFormat="0'%'" ShowLegend="True" charts:ChartAxisAppearance.Presentation="Standard"
                                   AutomationProperties.Name="CPU utilization" />
                               """;
        var cpuDataSource = """
                            public BarChartData CpuData { get; } = new(
                                ["CPU 0", "CPU 1", "CPU 2", "CPU 3", "CPU 4", "CPU 5"],
                                [
                                    new BarChartSeries("Before", [24, 81, 40, 58, 71, 32]),
                                    new BarChartSeries("After", [32, 93, 54, 75, 64, 41]),
                                ]);
                            """;
        this.CpuExample.CSharp = """
                                 private ChartAxisOptions CpuAxis { get; } = new() { ReferenceLines = (ChartReferenceLine[])[new(85, "85% threshold")] };


                                 """
                                 + cpuDataSource;
        this.CompactCpuExample.Xaml = """
                                      <charts:BarChart Height="128" Data="{x:Bind CpuData}" Maximum="100"
                                          charts:ChartAxisAppearance.Presentation="Hidden"
                                          charts:ChartLegend.Layout="Vertical" charts:ChartLegend.ItemSpacing="4" ValueFormat="0'%'"
                                          AutomationProperties.Name="Compact CPU comparison by logical processor" />
                                      """;
        this.CompactCpuExample.CSharp = cpuDataSource;
        this.StorageExample.Xaml = """
                                   <charts:BarChart x:Name="StorageChart" Height="245" Orientation="Horizontal"
                                       ValueFormat="0.#' GB'" AutomationProperties.Name="Weekly storage changes" />
                                   """;
        this.StorageExample.CSharp = """
                                     StorageChart.Data = new BarChartData(
                                         ["Apps", "Documents", "Downloads", "Media"],
                                         [
                                             new BarChartSeries("Previous week", [6, 2, -7, 3]),
                                             new BarChartSeries("This week", [3, 8, -4, -5]),
                                         ]);
                                     """;
        this.SignedStackExample.Xaml = """
                                       <charts:BarChart x:Name="RevenueChart" Height="245" Mode="Stacked" SegmentCornerRadius="4"
                                           ValueFormat="0'k'" AutomationProperties.Name="Monthly revenue and deductions in thousands" />
                                       """;
        this.SignedStackExample.CSharp = """
                                         RevenueChart.Data = new BarChartData(
                                             ["January", "February", "March"],
                                             [
                                                 new BarChartSeries("Retail", [40, 32, 56]),
                                                 new BarChartSeries("Subscriptions", [20, 24, 18]),
                                                 new BarChartSeries("Refunds", [-12, -8, -14]),
                                                 new BarChartSeries("Credits", [-4, -6, -7]),
                                             ]);
                                         """;
        this.PercentStackExample.Xaml = """
                                        <charts:BarChart x:Name="SignUpChart" Height="245" Mode="Stacked100" Orientation="Horizontal"
                                            SegmentGap="3" SegmentCornerRadius="4" SegmentRounding="OuterCorners"
                                            ValueFormat="0" AutomationProperties.Name="Share of sign-ups by platform and month" />
                                        """;
        this.PercentStackExample.CSharp = """
                                          SignUpChart.Data = new BarChartData(
                                              ["January", "February", "March"],
                                              [
                                                  new BarChartSeries("Web", [30, 50, 10]),
                                                  new BarChartSeries("Desktop", [10, 30, 50]),
                                                  new BarChartSeries("Mobile", [10, 20, 20]),
                                              ]);
                                          """;
    }

    private ChartAxisOptions GetCpuAxis(int index)
    {
        return this.GetMode(index) == BarChartMode.Stacked100 ? this.PercentCpuAxis : this.CpuAxis;
    }

    private BarChartMode GetMode(int index)
    {
        return index switch
        {
            1 => BarChartMode.Stacked,
            2 => BarChartMode.Stacked100,
            _ => BarChartMode.Grouped,
        };
    }

    private void OnInspectionModeChanged(object sender, SelectionChangedEventArgs args)
    {
        if (this.CpuChart is not null && sender is ComboBox { SelectedIndex: >= 0 } picker)
        {
            this.CpuChart.InspectionMode = (BarChartInspectionMode)picker.SelectedIndex;
        }
    }

    private void OnSignedOrientationChanged(object sender, RoutedEventArgs args)
    {
        if (this.SignedStackChart is not null)
        {
            this.SignedStackChart.Orientation = this.GetOrientation(((ToggleSwitch)sender).IsOn);
        }
    }

    private void OnPercentOrientationChanged(object sender, RoutedEventArgs args)
    {
        if (this.PercentStackChart is not null)
        {
            this.PercentStackChart.Orientation = this.GetOrientation(((ToggleSwitch)sender).IsOn);
        }
    }

    private Orientation GetOrientation(bool horizontal)
    {
        return horizontal ? Orientation.Horizontal : Orientation.Vertical;
    }

    private BarChartSegmentRounding GetSegmentRounding(int index)
    {
        return index == 1 ? BarChartSegmentRounding.AllCorners : BarChartSegmentRounding.OuterCorners;
    }
}
