// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using JPSoftworks.DesktopToolkit.Charts;
using JPSoftworks.DesktopToolkit.Controls.Demo.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Pages;

public sealed partial class ChartLegendsPage : Page
{
    private bool _alternateValues;

    public LineChartData ThroughputData { get; } = new(
    [
        new("API", [new(0, 38), new(1, 51), new(2, 47), new(3, 66), new(4, 72)]),
        new("Worker", [new(0, 24), new(1, 32), new(2, 40), new(3, 38), new(4, 52)]),
        new("Cache", [new(0, 15), new(1, 22), new(2, 19), new(3, 29), new(4, 34)]),
    ]);

    public ChartLegendsPage()
    {
        this.InitializeComponent();
        this.StorageChart.AddSeries("Applications", SamplePalette.Blue);
        this.StorageChart.AddSeries("Documents", SamplePalette.Purple);
        this.StorageChart.AddSeries("Available", SamplePalette.Slate);
        this.StorageChart.SetValues(42, 26, 32);

        this.BuiltInExample.Xaml = """
                                   <charts:LineChart x:Name="ThroughputChart" Data="{x:Bind ThroughputData}" ShowLegend="True"
                                       charts:ChartLegend.Placement="Bottom"
                                       charts:ChartLegend.Layout="Horizontal"
                                       charts:ChartLegend.ValueMode="Value"
                                       charts:ChartLegend.HighlightOnHover="True" />
                                   """;
        this.BuiltInExample.CSharp = """
                                     public LineChartData ThroughputData { get; } = new(
                                     [
                                         new("API", [new(0, 38), new(1, 51), new(2, 47), new(3, 66), new(4, 72)]),
                                         new("Worker", [new(0, 24), new(1, 32), new(2, 40), new(3, 38), new(4, 52)]),
                                         new("Cache", [new(0, 15), new(1, 22), new(2, 19), new(3, 29), new(4, 34)]),
                                     ]);

                                     private void SetVerticalLegend() =>
                                         ChartLegend.SetLayout(ThroughputChart, ChartLegendLayout.Vertical);
                                     """;
        this.DetachedExample.Xaml = """
                                    <Grid ColumnDefinitions="*,Auto" ColumnSpacing="12">
                                        <charts:DoughnutGraph x:Name="StorageChart" ShowLegend="False"
                                            ValueFormat="0' GB'" charts:ChartLegend.ValueMode="Value"
                                            charts:ChartLegend.Layout="Vertical"
                                            charts:ChartLegend.HighlightOnHover="True" />
                                        <charts:ChartLegendView Grid.Column="1" Chart="{x:Bind StorageChart}"
                                            MaxHeight="145" VerticalAlignment="Center" />
                                    </Grid>
                                    """;
        this.DetachedExample.CSharp = """
                                      // After InitializeComponent.
                                      StorageChart.AddSeries("Applications", Windows.UI.Color.FromArgb(255, 0, 120, 212));
                                      StorageChart.AddSeries("Documents", Windows.UI.Color.FromArgb(255, 156, 93, 216));
                                      StorageChart.AddSeries("Available", Windows.UI.Color.FromArgb(255, 100, 116, 139));
                                      StorageChart.SetValues(42, 26, 32);
                                      """;
    }

    private ChartLegendLayout GetLegendLayout(int index)
    {
        return index < 0 ? ChartLegendLayout.Auto : (ChartLegendLayout)index;
    }

    private void OnPlacementChanged(object sender, SelectionChangedEventArgs args)
    {
        if (this.ThroughputChart is not null && sender is ComboBox { SelectedIndex: >= 0 } placement)
        {
            ChartLegend.SetPlacement(this.ThroughputChart, (ChartLegendPlacement)placement.SelectedIndex);
        }
    }

    private void OnStorageLayoutSizeChanged(object sender, SizeChangedEventArgs args)
    {
        this.UpdateDetachedPlacement();
    }

    private void OnDetachedPlacementChanged(object sender, SelectionChangedEventArgs args)
    {
        this.UpdateDetachedPlacement();
    }

    private void UpdateDetachedPlacement()
    {
        if (this.StorageLayout is null || this.LegendPanel is null || this.DetachedPlacement is null)
        {
            return;
        }

        var width = this.StorageLayout.ActualWidth;
        var below = this.DetachedPlacement.SelectedIndex == 2 || width < 360;
        var start = this.DetachedPlacement.SelectedIndex == 0;
        Grid.SetRow(this.LegendPanel, below ? 1 : 0);
        Grid.SetColumn(this.LegendPanel, below || start ? 0 : 2);
        Grid.SetColumnSpan(this.LegendPanel, below ? 3 : 1);
        this.LegendPanel.MaxWidth = below ? double.PositiveInfinity : width / 2;
        this.LegendPanel.Margin = below ? new Thickness(0, 12, 0, 0) : start ? new Thickness(0, 0, 12, 0) : new Thickness(12, 0, 0, 0);
    }

    private void OnChangeValues(object sender, RoutedEventArgs args)
    {
        this._alternateValues = !this._alternateValues;
        this.StorageChart.SetValues(this._alternateValues ? [30, 45, 25] : [42, 26, 32]);
    }
}
