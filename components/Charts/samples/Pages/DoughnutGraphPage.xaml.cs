// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using JPSoftworks.DesktopToolkit.Charts;
using JPSoftworks.DesktopToolkit.Controls.Demo.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Pages;

public sealed partial class DoughnutGraphPage : Page
{
    private static readonly DoughnutGraphSeries[] StorageSeries =
        [new("Applications", SamplePalette.Blue), new("Documents", SamplePalette.Purple), new("Available", SamplePalette.Slate)];

    private bool _initialized;
    private bool _alternateValues;

    public DoughnutGraphPage()
    {
        this.InitializeComponent();
        foreach (var series in StorageSeries)
        {
            this.ShapeGraph.AddSeries(series.Name, series.Color);
        }

        this.Graph.RingThickness = 18;
        this.ShapeGraph.RingThickness = this.ThicknessSlider.Value;
        this.ShapeGraph.SetValues(42, 26, 32);
        this.ValuesExample.Xaml = """
                                  <charts:DoughnutGraph x:Name="Graph" Width="240" Height="285" ShowLegend="True"
                                      Data="{x:Bind StorageData}" ValueFormat="0.#' GB'" CenterValue="68%" CenterLabel="used"
                                      AutomationProperties.Name="Storage composition" />
                                  """;
        this.ValuesExample.CSharp = """
                                    using Windows.UI;

                                    public DoughnutGraphData StorageData { get; } = new(
                                        [new("Applications", Color.FromArgb(255, 0, 120, 212)),
                                         new("Documents", Color.FromArgb(255, 156, 93, 216)),
                                         new("Available", Color.FromArgb(255, 96, 117, 138))],
                                        [42, 26, 32]);
                                    """;
        this.PieExample.Xaml = """
                               <charts:DoughnutGraph x:Name="Pie" MaxWidth="540" Height="270"
                                   Mode="Pie" ShowLegend="True" Data="{x:Bind StorageData}" ValueFormat="0.#' GB'"
                                   AutomationProperties.Name="Storage composition pie" />
                               """;
        this.PieExample.CSharp = """
                                 using Windows.UI;

                                 public DoughnutGraphData StorageData { get; } = new(
                                     [new("Applications", Color.FromArgb(255, 0, 120, 212)),
                                      new("Documents", Color.FromArgb(255, 156, 93, 216)),
                                      new("Available", Color.FromArgb(255, 96, 117, 138))],
                                     [42, 26, 32]);
                                 """;
        this.ShapeExample.Xaml = """
                                 <charts:DoughnutGraph x:Name="Graph" Width="240" Height="240"
                                     CenterValue="68%" CenterLabel="used" />
                                 """;
        this.ShapeExample.CSharp = """
                                   // Configure the slices before the first value update.
                                   Graph.RingThickness = 18;
                                   Graph.GapAngle = 3;
                                   Graph.TransitionDuration = TimeSpan.FromMilliseconds(420);
                                   Graph.SetValues(42, 26, 32);
                                   // A subsequent update retargets any animation already in progress.
                                   Graph.SetValues(25, 50, 25);
                                   """;
        this._initialized = true;
        this.UpdateValues();
    }

    private void OnValuesChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (this._initialized)
        {
            this.UpdateValues();
        }
    }

    private void UpdateValues()
    {
        var apps = double.IsFinite(this.AppsValue.Value) ? this.AppsValue.Value : 0;
        var docs = double.IsFinite(this.DocsValue.Value) ? this.DocsValue.Value : 0;
        var free = double.IsFinite(this.FreeValue.Value) ? this.FreeValue.Value : 0;
        var total = apps + docs + free;
        this.Graph.Data = this.PieGraph.Data = new DoughnutGraphData(StorageSeries, [apps, docs, free]);
        this.Graph.CenterValue = total == 0 ? "0%" : $"{(apps + docs) / total:0%}";
        var summary = $"Applications {apps:0.#} GB, Documents {docs:0.#} GB, Available {free:0.#} GB";
        AutomationProperties.SetHelpText(this.Graph, summary);
        AutomationProperties.SetHelpText(this.PieGraph, summary);
    }

    private void OnShapeChanged(object sender, RangeBaseValueChangedEventArgs args)
    {
        if (!this._initialized)
        {
            return;
        }

        this.ShapeGraph.RingThickness = this.ThicknessSlider.Value;
        this.ShapeGraph.GapAngle = this.GapSlider.Value;
    }

    private void OnAnimationClick(object sender, RoutedEventArgs args)
    {
        this.ShapeGraph.TransitionDuration = this.AnimationBox.IsChecked == true ? TimeSpan.FromMilliseconds(420) : TimeSpan.Zero;
    }

    private void OnRetargetClick(object sender, RoutedEventArgs args)
    {
        this._alternateValues = !this._alternateValues;
        this.ShapeGraph.SetValues(this._alternateValues ? [25, 50, 25] : [42, 26, 32]);
        this.ShapeGraph.CenterValue = this._alternateValues ? "75%" : "68%";
    }
}
