// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using JPSoftworks.DesktopToolkit.Charts;
using JPSoftworks.DesktopToolkit.Controls.Demo.Services;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Pages;

public sealed partial class SpiderChartPage : Page
{
    public SpiderChartData ComparisonData { get; } = new(
    [
        new("Battery", 0, 20) { ValueFormat = "0' hours'" },
        new("Display", 0, 10),
        new("Graphics", 0, 10),
        new("Performance", 0, 10),
        new("Ports", 0, 8),
        new("Portability", 0, 10),
    ], [new("Travel", [17, 6, 4, 5, 3, 9]), new("Workstation", [10, 9, 9, 9, 7, 5])]);

    public SpiderChartPage()
    {
        this.InitializeComponent();
        ChartContextMenu.SetProvider(this.ComparisonChart, ChartDetailsMenuProvider.Instance);
        this.ComparisonExample.Xaml = """
                                      <charts:SpiderChart Height="340" Data="{x:Bind ComparisonData}" />
                                      """;
        this.ComparisonExample.CSharp = """
                                        public SpiderChartData ComparisonData { get; } = new(
                                            [new("Battery", 0, 20), new("Display", 0, 10), new("Graphics", 0, 10),
                                             new("Performance", 0, 10), new("Ports", 0, 8), new("Portability", 0, 10)],
                                            [new("Travel", [17, 6, 4, 5, 3, 9]), new("Workstation", [10, 9, 9, 9, 7, 5])]);
                                        """;
    }

    private int GetRingCount(double value)
    {
        return (int)value;
    }
}
