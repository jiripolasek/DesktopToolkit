// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Supplies item-specific context menus for chart controls.</summary>
public static class ChartContextMenu
{
    /// <summary>Identifies the context menu provider attached property.</summary>
    public static readonly DependencyProperty ProviderProperty = DependencyProperty.RegisterAttached(
        "Provider", typeof(IChartContextMenuProvider), typeof(ChartContextMenu), new PropertyMetadata(null));

    /// <summary>Gets the provider for the chart's data items.</summary>
    public static IChartContextMenuProvider? GetProvider(DependencyObject chart)
    {
        return (IChartContextMenuProvider?)chart.GetValue(ProviderProperty);
    }

    /// <summary>Sets the provider for the chart's data items. Null disables item-specific menus.</summary>
    public static void SetProvider(DependencyObject chart, IChartContextMenuProvider? provider)
    {
        chart.SetValue(ProviderProperty, provider);
    }
}
