// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Controls the layout and interaction of a chart legend.</summary>
public static class ChartLegend
{
    /// <summary>Identifies the Placement attached property.</summary>
    public static readonly DependencyProperty PlacementProperty = DependencyProperty.RegisterAttached(
        "Placement", typeof(ChartLegendPlacement), typeof(ChartLegend), new PropertyMetadata(ChartLegendPlacement.Auto));

    /// <summary>Identifies the Layout attached property.</summary>
    public static readonly DependencyProperty LayoutProperty = DependencyProperty.RegisterAttached(
        "Layout", typeof(ChartLegendLayout), typeof(ChartLegend), new PropertyMetadata(ChartLegendLayout.Auto));

    /// <summary>Identifies the Alignment attached property.</summary>
    public static readonly DependencyProperty AlignmentProperty = DependencyProperty.RegisterAttached(
        "Alignment", typeof(HorizontalAlignment), typeof(ChartLegend), new PropertyMetadata(HorizontalAlignment.Left));

    /// <summary>Identifies the ItemSpacing attached property.</summary>
    public static readonly DependencyProperty ItemSpacingProperty = DependencyProperty.RegisterAttached(
        "ItemSpacing", typeof(double), typeof(ChartLegend), new PropertyMetadata(12d));

    /// <summary>Identifies the ValueMode attached property.</summary>
    public static readonly DependencyProperty ValueModeProperty = DependencyProperty.RegisterAttached(
        "ValueMode", typeof(ChartLegendValueMode), typeof(ChartLegend), new PropertyMetadata(ChartLegendValueMode.Default));

    /// <summary>Identifies the HighlightOnHover attached property.</summary>
    public static readonly DependencyProperty HighlightOnHoverProperty = DependencyProperty.RegisterAttached(
        "HighlightOnHover", typeof(bool), typeof(ChartLegend), new PropertyMetadata(false));

    /// <summary>Identifies the ItemTemplate attached property.</summary>
    public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.RegisterAttached(
        "ItemTemplate", typeof(DataTemplate), typeof(ChartLegend), new PropertyMetadata(null));

    /// <summary>Gets the template for a legend entry. Its data is a <see cref="ChartLegendItem"/>.</summary>
    [WinRT.DynamicWindowsRuntimeCast(typeof(DataTemplate))]
    public static DataTemplate? GetItemTemplate(DependencyObject chart)
    {
        return (DataTemplate?)chart.GetValue(ItemTemplateProperty);
    }

    /// <summary>Sets the template for a legend entry. Null uses the built-in rendering.</summary>
    public static void SetItemTemplate(DependencyObject chart, DataTemplate? template)
    {
        chart.SetValue(ItemTemplateProperty, template);
    }

    /// <summary>Gets the position of the built-in legend relative to the plot.</summary>
    public static ChartLegendPlacement GetPlacement(DependencyObject chart)
    {
        return (ChartLegendPlacement)chart.GetValue(PlacementProperty);
    }

    /// <summary>Sets the position of the built-in legend relative to the plot.</summary>
    public static void SetPlacement(DependencyObject chart, ChartLegendPlacement value)
    {
        chart.SetValue(PlacementProperty, value);
    }

    /// <summary>Gets the requested layout for series entries.</summary>
    public static ChartLegendLayout GetLayout(DependencyObject chart)
    {
        return (ChartLegendLayout)chart.GetValue(LayoutProperty);
    }

    /// <summary>Sets whether series entries wrap in rows or form one column.</summary>
    public static void SetLayout(DependencyObject chart, ChartLegendLayout value)
    {
        chart.SetValue(LayoutProperty, value);
    }

    /// <summary>Gets the alignment of the legend within its container.</summary>
    [WinRT.DynamicWindowsRuntimeCast(typeof(HorizontalAlignment))]
    public static HorizontalAlignment GetAlignment(DependencyObject chart)
    {
        return (HorizontalAlignment)chart.GetValue(AlignmentProperty);
    }

    /// <summary>Sets the alignment of the legend within its container.</summary>
    public static void SetAlignment(DependencyObject chart, HorizontalAlignment value)
    {
        chart.SetValue(AlignmentProperty, value);
    }

    /// <summary>Gets the spacing in DIPs between entries in the primary layout direction.</summary>
    public static double GetItemSpacing(DependencyObject chart)
    {
        return (double)chart.GetValue(ItemSpacingProperty);
    }

    /// <summary>Sets the spacing in DIPs between entries in the primary layout direction.</summary>
    public static void SetItemSpacing(DependencyObject chart, double value)
    {
        chart.SetValue(ItemSpacingProperty, value);
    }

    /// <summary>Gets the legend value presentation.</summary>
    public static ChartLegendValueMode GetValueMode(DependencyObject chart)
    {
        return (ChartLegendValueMode)chart.GetValue(ValueModeProperty);
    }

    /// <summary>Sets whether a supported chart shows a value in each legend entry.</summary>
    public static void SetValueMode(DependencyObject chart, ChartLegendValueMode value)
    {
        chart.SetValue(ValueModeProperty, value);
    }

    /// <summary>Gets whether pointing at a series legend entry highlights its marks.</summary>
    public static bool GetHighlightOnHover(DependencyObject chart)
    {
        return (bool)chart.GetValue(HighlightOnHoverProperty);
    }

    /// <summary>Sets whether pointing at a series legend entry highlights its marks.</summary>
    public static void SetHighlightOnHover(DependencyObject chart, bool value)
    {
        chart.SetValue(HighlightOnHoverProperty, value);
    }

    internal static void RegisterCallbacks(DependencyObject chart, DependencyPropertyChangedCallback update)
    {
        chart.RegisterPropertyChangedCallback(PlacementProperty, update);
        chart.RegisterPropertyChangedCallback(LayoutProperty, update);
        chart.RegisterPropertyChangedCallback(AlignmentProperty, update);
        chart.RegisterPropertyChangedCallback(ItemSpacingProperty, update);
        chart.RegisterPropertyChangedCallback(ValueModeProperty, update);
        chart.RegisterPropertyChangedCallback(HighlightOnHoverProperty, update);
        chart.RegisterPropertyChangedCallback(ItemTemplateProperty, update);
    }

    internal static bool IsSide(ChartLegendPlacement placement)
    {
        return placement is ChartLegendPlacement.Start or ChartLegendPlacement.End;
    }

    internal static void ApplyPlacement(Microsoft.UI.Xaml.Controls.Control chart, ChartLegendPlacement placement)
    {
        VisualStateManager.GoToState(
            chart, placement switch
            {
                ChartLegendPlacement.Top => "LegendAbove",
                ChartLegendPlacement.Start => "LegendStart",
                ChartLegendPlacement.End => "LegendBeside",
                _ => "LegendBelow",
            }, false);
    }
}

/// <summary>Positions a built-in legend relative to its chart's plot.</summary>
public enum ChartLegendPlacement
{
    /// <summary>Uses the chart's default or responsive placement.</summary>
    Auto,

    /// <summary>Places the legend above the plot.</summary>
    Top,

    /// <summary>Places the legend below the plot.</summary>
    Bottom,

    /// <summary>Places the legend on the leading side of the plot.</summary>
    Start,

    /// <summary>Places the legend on the trailing side of the plot.</summary>
    End,
}

/// <summary>Specifies the arrangement of a series legend.</summary>
public enum ChartLegendLayout
{
    /// <summary>Uses the chart's responsive layout or its default orientation.</summary>
    Auto,

    /// <summary>Packs entries into rows that wrap to fit the available width.</summary>
    Horizontal,

    /// <summary>Stacks the entries in one column.</summary>
    Vertical,
}

/// <summary>Specifies how values appear in a series legend.</summary>
public enum ChartLegendValueMode
{
    /// <summary>Uses the individual chart's default.</summary>
    Default,

    /// <summary>Shows the series name alone.</summary>
    Hidden,

    /// <summary>Shows the chart's value for each series when one is available.</summary>
    Value,
}
