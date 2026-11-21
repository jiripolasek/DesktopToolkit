// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Provides shared presentation settings and brush overrides for chart axes.</summary>
public static class ChartAxisAppearance
{
    /// <summary>Identifies the Presentation attached property.</summary>
    public static readonly DependencyProperty PresentationProperty = DependencyProperty.RegisterAttached(
        "Presentation", typeof(ChartAxisPresentation), typeof(ChartAxisAppearance), new PropertyMetadata(ChartAxisPresentation.Standard));

    /// <summary>Identifies the LabelBrush attached property.</summary>
    public static readonly DependencyProperty LabelBrushProperty = DependencyProperty.RegisterAttached(
        "LabelBrush", typeof(Brush), typeof(ChartAxisAppearance), new PropertyMetadata(null));

    /// <summary>Identifies the LineBrush attached property.</summary>
    public static readonly DependencyProperty LineBrushProperty = DependencyProperty.RegisterAttached(
        "LineBrush", typeof(Brush), typeof(ChartAxisAppearance), new PropertyMetadata(null));

    /// <summary>Identifies the GridLineBrush attached property.</summary>
    public static readonly DependencyProperty GridLineBrushProperty = DependencyProperty.RegisterAttached(
        "GridLineBrush", typeof(Brush), typeof(ChartAxisAppearance), new PropertyMetadata(null));

    /// <summary>Gets the Cartesian grid brush. Null uses the chart border brush.</summary>
    public static Brush? GetGridLineBrush(DependencyObject chart)
    {
        return (Brush?)chart.GetValue(GridLineBrushProperty);
    }

    /// <summary>Sets the grid brush for bar, line, and scatter charts.</summary>
    public static void SetGridLineBrush(DependencyObject chart, Brush? brush)
    {
        chart.SetValue(GridLineBrushProperty, brush);
    }

    /// <summary>Gets the chart-wide setting for axis labels, axis lines, and grid lines.</summary>
    public static ChartAxisPresentation GetPresentation(DependencyObject chart)
    {
        return (ChartAxisPresentation)chart.GetValue(PresentationProperty);
    }

    /// <summary>Sets the axis presentation for a bar, line, scatter, spider, or heatmap chart.</summary>
    public static void SetPresentation(DependencyObject chart, ChartAxisPresentation presentation)
    {
        chart.SetValue(PresentationProperty, presentation);
    }

    /// <summary>Gets the axis label brush. Null falls back to the control's foreground.</summary>
    public static Brush? GetLabelBrush(DependencyObject chart)
    {
        return (Brush?)chart.GetValue(LabelBrushProperty);
    }

    /// <summary>Sets the brush for axis labels without changing the legend.</summary>
    public static void SetLabelBrush(DependencyObject chart, Brush? brush)
    {
        chart.SetValue(LabelBrushProperty, brush);
    }

    /// <summary>Gets the axis line brush. Null falls back to the control's foreground.</summary>
    public static Brush? GetLineBrush(DependencyObject chart)
    {
        return (Brush?)chart.GetValue(LineBrushProperty);
    }

    /// <summary>Sets the brush for baselines and radial axis guides.</summary>
    public static void SetLineBrush(DependencyObject chart, Brush? brush)
    {
        chart.SetValue(LineBrushProperty, brush);
    }

    internal static void RegisterCallbacks(DependencyObject chart, DependencyPropertyChangedCallback redraw)
    {
        chart.RegisterPropertyChangedCallback(PresentationProperty, redraw);
        chart.RegisterPropertyChangedCallback(LabelBrushProperty, redraw);
        chart.RegisterPropertyChangedCallback(LineBrushProperty, redraw);
        chart.RegisterPropertyChangedCallback(GridLineBrushProperty, redraw);
    }
}
