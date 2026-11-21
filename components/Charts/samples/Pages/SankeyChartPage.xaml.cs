// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using JPSoftworks.DesktopToolkit.Charts;
using JPSoftworks.DesktopToolkit.Controls.Demo.Services;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Pages;

public sealed partial class SankeyChartPage : Page
{
    public SankeyChartData Acquisition { get; } = new(
    [
        new("search", "Search"), new("referral", "Referral"), new("direct", "Direct"),
        new("product", "Product page"), new("pricing", "Pricing page"),
        new("trial", "Trial"), new("purchase", "Purchase"), new("browse", "Browse only"),
    ], [
        new("search", "product", 490), new("search", "pricing", 150),
        new("referral", "product", 220), new("referral", "pricing", 90),
        new("direct", "product", 190), new("direct", "pricing", 60),
        new("product", "trial", 330), new("product", "purchase", 110), new("product", "browse", 460),
        new("pricing", "trial", 60), new("pricing", "purchase", 110), new("pricing", "browse", 130),
    ]);

    public SankeyChartPage()
    {
        this.InitializeComponent();
        ChartContextMenu.SetProvider(this.Diagram, ChartDetailsMenuProvider.Instance);
        this.FlowExample.Xaml = """
                                <charts:SankeyChart Height="356" Data="{x:Bind Acquisition}" ValueFormat="N0"
                                    NodeWidth="16" NodeSpacing="14" ShowLabels="True"
                                    AutomationProperties.Name="Visitors by channel, landing page, and outcome" />
                                """;
        this.FlowExample.CSharp = """
                                  public SankeyChartData Acquisition { get; } = new(
                                  [
                                      new("search", "Search"), new("referral", "Referral"), new("direct", "Direct"),
                                      new("product", "Product page"), new("pricing", "Pricing page"),
                                      new("trial", "Trial"), new("purchase", "Purchase"), new("browse", "Browse only"),
                                  ],
                                  [
                                      new("search", "product", 490), new("search", "pricing", 150),
                                      new("referral", "product", 220), new("referral", "pricing", 90),
                                      new("direct", "product", 190), new("direct", "pricing", 60),
                                      new("product", "trial", 330), new("product", "purchase", 110), new("product", "browse", 460),
                                      new("pricing", "trial", 60), new("pricing", "purchase", 110), new("pricing", "browse", 130),
                                  ]);
                                  """;
    }
}
