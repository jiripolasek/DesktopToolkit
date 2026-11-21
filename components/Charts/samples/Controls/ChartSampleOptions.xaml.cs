// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using JPSoftworks.DesktopToolkit.Charts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.UI;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Controls;

public sealed partial class ChartSampleOptions : UserControl
{
    public static readonly DependencyProperty TargetProperty = DependencyProperty.Register(
        nameof(Target), typeof(Control), typeof(ChartSampleOptions),
        new PropertyMetadata(null, static (sender, _) => ((ChartSampleOptions)sender).UpdateFromTarget()));

    private static readonly ChartColorSet?[] Palettes =
    [
        null, ChartPalette.Vivid, ChartPalette.Soft, ChartPalette.Earth,
        ChartPalette.CreateFromAccent(Color.FromArgb(255, 96, 74, 206), 6)
    ];

    private DependencyProperty? _legendProperty;
    private DependencyProperty? _gridProperty;
    private bool _updating = true;

    public Control? Target
    {
        get => (Control?)this.GetValue(TargetProperty);
        set => this.SetValue(TargetProperty, value);
    }

    public ChartSampleOptions()
    {
        this.InitializeComponent();
        this._updating = false;
        this.Loaded += (_, _) => this.UpdateFromTarget();
    }

    private void UpdateFromTarget()
    {
        if (this._updating || this.Target is not { } chart)
        {
            return;
        }

        this._updating = true;
        (this._legendProperty, this._gridProperty) = chart switch
        {
            BarChart => (BarChart.ShowLegendProperty, BarChart.ShowGridProperty),
            LineChart => (LineChart.ShowLegendProperty, LineChart.ShowGridProperty),
            ScatterChart => (ScatterChart.ShowLegendProperty, ScatterChart.ShowGridProperty),
            SpiderChart => (SpiderChart.ShowLegendProperty, null),
            HeatmapChart => (HeatmapChart.ShowLegendProperty, null),
            VerticalUsageBar => (VerticalUsageBar.ShowLegendProperty, null),
            DoughnutGraph => (DoughnutGraph.ShowLegendProperty, null),
            FunnelChart { Presentation: FunnelChartPresentation.Flow } => (FunnelChart.ShowLegendProperty, null),
            _ => (null, null),
        };
        var hasAxes = chart is BarChart or LineChart or ScatterChart or SpiderChart or HeatmapChart;
        var hasPalette = chart is BarChart or LineChart or ScatterChart or SpiderChart or TreemapChart or IcicleChart or SunburstChart
            or SankeyChart or FunnelChart;
        this.AxesBox.Visibility = VisibilityFor(hasAxes);
        this.SubtleAxesItem.Visibility = VisibilityFor(chart is not HeatmapChart);
        this.PaletteBox.Visibility = VisibilityFor(hasPalette);
        var hasDisplayOptions = hasAxes || hasPalette;
        var hasLegend = this._legendProperty is not null;
        this.DisplayItem.Visibility = VisibilityFor(hasDisplayOptions);
        this.LegendItem.Visibility = VisibilityFor(hasLegend);
        this.Sections.Visibility = VisibilityFor(hasDisplayOptions || hasLegend);
        if (this.Sections.SelectedItem is not { Visibility: Visibility.Visible })
        {
            this.Sections.SelectedItem = hasDisplayOptions ? this.DisplayItem : hasLegend ? this.LegendItem : this.InspectorItem;
        }

        this.UpdateSections();

        this.LegendOptions.Visibility = VisibilityFor(hasLegend);
        var hasSeriesLegend = chart is not HeatmapChart;
        this.LegendLayoutBox.Visibility = VisibilityFor(hasSeriesLegend);
        this.LegendSpacingSlider.Visibility = VisibilityFor(hasSeriesLegend);
        this.LegendHighlightSwitch.Visibility = VisibilityFor(hasSeriesLegend);
        this.LegendValueBox.Visibility = VisibilityFor(hasSeriesLegend && chart is not SpiderChart);

        var axes = ChartAxisAppearance.GetPresentation(chart);
        this.AxesBox.SelectedIndex = (int)axes;
        this.UpdateGridVisibility(axes);
        this.GridSwitch.IsOn = this._gridProperty is { } grid && (bool)chart.GetValue(grid);
        this.PaletteBox.SelectedIndex = Array.IndexOf(Palettes, ChartPalette.GetColors(chart));
        this.LegendSwitch.IsOn = this._legendProperty is { } legend && (bool)chart.GetValue(legend);
        this.LegendLayoutBox.SelectedIndex = (int)ChartLegend.GetLayout(chart);
        this.LegendAlignmentBox.SelectedIndex = ChartLegend.GetAlignment(chart) switch
        {
            HorizontalAlignment.Center => 1,
            HorizontalAlignment.Right => 2,
            _ => 0,
        };
        this.LegendSpacingSlider.Value = ChartLegend.GetItemSpacing(chart);
        this.LegendValueBox.SelectedIndex = (int)ChartLegend.GetValueMode(chart);
        this.LegendHighlightSwitch.IsOn = ChartLegend.GetHighlightOnHover(chart);
        this.InspectorSwitch.IsOn = ChartInspection.GetIsEnabled(chart);
        this.HoverDelaySlider.Value = ChartInspection.GetHoverDelay(chart).TotalMilliseconds;
        this.SlideDurationSlider.Value = ChartInspection.GetTransitionDuration(chart).TotalMilliseconds;
        this._updating = false;
    }

    private void OnSectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (!this._updating)
        {
            this.UpdateSections();
        }
    }

    private void UpdateSections()
    {
        this.DisplaySection.Visibility = VisibilityFor(ReferenceEquals(this.Sections.SelectedItem, this.DisplayItem));
        this.LegendSection.Visibility = VisibilityFor(ReferenceEquals(this.Sections.SelectedItem, this.LegendItem));
        this.InspectorSection.Visibility = VisibilityFor(ReferenceEquals(this.Sections.SelectedItem, this.InspectorItem));
    }

    private void OnAxesChanged(object sender, SelectionChangedEventArgs args)
    {
        if (!this._updating && this.Target is { } chart && this.AxesBox.SelectedIndex >= 0)
        {
            var axes = (ChartAxisPresentation)this.AxesBox.SelectedIndex;
            ChartAxisAppearance.SetPresentation(chart, axes);
            this.UpdateGridVisibility(axes);
        }
    }

    private void OnPaletteChanged(object sender, SelectionChangedEventArgs args)
    {
        if (!this._updating && this.Target is { } chart && this.PaletteBox.SelectedIndex >= 0)
        {
            ChartPalette.SetColors(chart, Palettes[this.PaletteBox.SelectedIndex]);
        }
    }

    private void OnGridToggled(object sender, RoutedEventArgs args)
    {
        this.ChangeToggle(this._gridProperty, (ToggleSwitch)sender);
    }

    private void OnLegendToggled(object sender, RoutedEventArgs args)
    {
        this.ChangeToggle(this._legendProperty, (ToggleSwitch)sender);
    }

    private void OnLegendLayoutChanged(object sender, SelectionChangedEventArgs args)
    {
        if (!this._updating && this.Target is { } chart && this.LegendLayoutBox.SelectedIndex >= 0)
        {
            ChartLegend.SetLayout(chart, (ChartLegendLayout)this.LegendLayoutBox.SelectedIndex);
        }
    }

    private void OnLegendAlignmentChanged(object sender, SelectionChangedEventArgs args)
    {
        if (!this._updating && this.Target is { } chart && this.LegendAlignmentBox.SelectedIndex >= 0)
        {
            ChartLegend.SetAlignment(
                chart, this.LegendAlignmentBox.SelectedIndex switch
                {
                    1 => HorizontalAlignment.Center,
                    2 => HorizontalAlignment.Right,
                    _ => HorizontalAlignment.Left,
                });
        }
    }

    private void OnLegendSpacingChanged(object sender, RangeBaseValueChangedEventArgs args)
    {
        if (!this._updating && this.Target is { } chart)
        {
            ChartLegend.SetItemSpacing(chart, args.NewValue);
        }
    }

    private void OnLegendValueChanged(object sender, SelectionChangedEventArgs args)
    {
        if (!this._updating && this.Target is { } chart && this.LegendValueBox.SelectedIndex >= 0)
        {
            ChartLegend.SetValueMode(chart, (ChartLegendValueMode)this.LegendValueBox.SelectedIndex);
        }
    }

    private void OnLegendHighlightToggled(object sender, RoutedEventArgs args)
    {
        this.ChangeToggle(ChartLegend.HighlightOnHoverProperty, (ToggleSwitch)sender);
    }

    private void ChangeToggle(DependencyProperty? property, ToggleSwitch toggle)
    {
        if (!this._updating && this.Target is { } chart && property is not null)
        {
            chart.SetValue(property, toggle.IsOn);
        }
    }

    private void OnInspectorToggled(object sender, RoutedEventArgs args)
    {
        if (!this._updating && this.Target is { } chart)
        {
            ChartInspection.SetIsEnabled(chart, ((ToggleSwitch)sender).IsOn);
        }
    }

    private void OnHoverDelayChanged(object sender, RangeBaseValueChangedEventArgs args)
    {
        if (!this._updating && this.Target is { } chart)
        {
            ChartInspection.SetHoverDelay(chart, TimeSpan.FromMilliseconds(args.NewValue));
        }
    }

    private void OnSlideDurationChanged(object sender, RangeBaseValueChangedEventArgs args)
    {
        if (!this._updating && this.Target is { } chart)
        {
            ChartInspection.SetTransitionDuration(chart, TimeSpan.FromMilliseconds(args.NewValue));
        }
    }

    private void UpdateGridVisibility(ChartAxisPresentation axes)
    {
        this.GridSwitch.Visibility = VisibilityFor(this._gridProperty is not null && axes == ChartAxisPresentation.Standard);
    }

    private static Visibility VisibilityFor(bool visible)
    {
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }
}
