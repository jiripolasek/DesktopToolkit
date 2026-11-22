// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using JPSoftworks.DesktopToolkit.Charts;
using JPSoftworks.DesktopToolkit.Controls.Demo.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Pages;

public sealed partial class FunnelChartPage : Page
{
    public FunnelChartData Conversion { get; } = new(
        new("Site visits", 24000), new("Trial starts", 7200), new("Accounts created", 3900), new("Activated trials", 1260),
        new("Paid subscriptions", 720));

    public FunnelChartData SupportRequests { get; } = FunnelChartData.FromSeries(
        ["Logged", "Triaged", "Assigned", "Resolved", "Closed"], new("Web", [840, 680, 545, 489, 414]),
        new("Email", [450, 390, 310, 256, 212]), new("Chat", [330, 292, 235, 175, 148]));

    public FunnelChartPage()
    {
        this.InitializeComponent();
        ChartContextMenu.SetProvider(this.Diagram, ChartDetailsMenuProvider.Instance);
        ChartContextMenu.SetProvider(this.CohortFlow, ChartDetailsMenuProvider.Instance);
        this.FlowExample.Xaml = """
                                <charts:FunnelChart Height="318" Data="{x:Bind Conversion}" ValueFormat="N0"
                                    StageAlignment="Left" IsTapered="True" StageSpacing="6" StageCornerRadius="6" ShowLabels="True"
                                    AutomationProperties.Name="Trial to paid subscription conversion stages" />
                                """;
        this.FlowExample.CSharp = """
                                  public FunnelChartData Conversion { get; } = new(
                                      new("Site visits", 24000),
                                      new("Trial starts", 7200),
                                      new("Accounts created", 3900),
                                      new("Activated trials", 1260),
                                      new("Paid subscriptions", 720));
                                  """;
        this.CohortExample.Xaml = """
                                  <charts:FunnelChart Height="290" Data="{x:Bind SupportRequests}" Presentation="Flow"
                                      charts:ChartLegend.HighlightOnHover="True" ValueFormat="N0" />
                                  """;
        this.CohortExample.CSharp = """
                                    public FunnelChartData SupportRequests { get; } = FunnelChartData.FromSeries(
                                        ["Logged", "Triaged", "Assigned", "Resolved", "Closed"],
                                        new("Web", [840, 680, 545, 489, 414]),
                                        new("Email", [450, 390, 310, 256, 212]),
                                        new("Chat", [330, 292, 235, 175, 148]));
                                    """;
    }

    private HorizontalAlignment GetStageAlignment(int index)
    {
        return index switch
        {
            0 => HorizontalAlignment.Left,
            2 => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Center,
        };
    }
}
