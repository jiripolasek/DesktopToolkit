// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using JPSoftworks.DesktopToolkit.Charts;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Pages;

public sealed partial class ScatterChartPage : Page
{
    public ScatterChartData RequestData { get; } = new(
    [
        new ScatterChartSeries(
            "Cached", [
                new(16, 24), new(4, 12), new(8, 21), new(16, 18), new(32, 29), new(48, 37),
                new(64, 32), new(64, 44), new(96, 52), new(112, 48), new(128, 61)
            ]),
        new ScatterChartSeries(
            "Uncached", [
                new(4, 48), new(8, 54), new(16, 62), new(16, 84, "Cold start"), new(32, 73),
                new(48, 81), new(64, 94), new(64, 83), new(96, 112), new(112, 104), new(128, 121)
            ]),
    ]);

    private ChartMilestones RequestMilestones { get; } = new(new ChartMilestone(16, "Cold start", y: 84));

    private ChartAxisOptions SizeAxis { get; } = new() { ReferenceLines = (ChartReferenceLine[])[new(64, "64 KB target")] };

    private ChartAxisOptions LatencyAxis { get; } = new() { ReferenceLines = (ChartReferenceLine[])[new(80, "80 ms budget")] };

    private ChartAxisOptions SubtleAxis { get; } = new() { LabelOpacity = 0.78 };

    public ScatterChartPage()
    {
        this.InitializeComponent();
        this.RequestExample.Xaml = """
                                   <charts:ScatterChart Height="280" Data="{x:Bind RequestData}" Milestones="{x:Bind RequestMilestones}"
                                       XAxis="{x:Bind SizeAxis}" YAxis="{x:Bind LatencyAxis}"
                                       XValueFormat="0' KB'" YValueFormat="0' ms'" />
                                   """;
        var requestDataSource = """
                                public ScatterChartData RequestData { get; } = new(
                                [
                                    new ScatterChartSeries("Cached", [new(4, 12), new(16, 24), new(16, 18)]),
                                    new ScatterChartSeries("Uncached", [new(4, 48), new(16, 84, "Cold start"), new(64, 94)]),
                                ]);
                                """;
        this.RequestExample.CSharp = """
                                     private ChartMilestones RequestMilestones { get; } = new(new ChartMilestone(16, "Cold start", y: 84));

                                     private ChartAxisOptions SizeAxis { get; } = new() { ReferenceLines = (ChartReferenceLine[])[new(64, "64 KB target")] };
                                     private ChartAxisOptions LatencyAxis { get; } = new() { ReferenceLines = (ChartReferenceLine[])[new(80, "80 ms budget")] };


                                     """
                                     + requestDataSource;
        this.SubtleExample.Xaml = """
                                  <charts:ScatterChart Height="176" Data="{x:Bind RequestData}"
                                      charts:ChartAxisAppearance.Presentation="Subtle" ShowGrid="False" MarkerSize="6"
                                      XAxis="{x:Bind SubtleAxis}" YAxis="{x:Bind SubtleAxis}"
                                      XValueFormat="0' KB'" YValueFormat="0' ms'"
                                      AutomationProperties.Name="Compact scatter plot of request size versus latency" />
                                  """;
        this.SubtleExample.CSharp = requestDataSource
                                    + """

                                      private ChartAxisOptions SubtleAxis { get; } = new() { LabelOpacity = 0.78 };
                                      """;
    }
}
