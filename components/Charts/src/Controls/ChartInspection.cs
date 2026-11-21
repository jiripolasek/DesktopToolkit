// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Controls interactive data inspection on a chart.</summary>
public static class ChartInspection
{
    /// <summary>Identifies the IsEnabled attached property. The default is true.</summary>
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(ChartInspection), new PropertyMetadata(true));

    /// <summary>Identifies the HoverDelay attached property. The default is 200 milliseconds.</summary>
    public static readonly DependencyProperty HoverDelayProperty = DependencyProperty.RegisterAttached(
        "HoverDelay", typeof(TimeSpan), typeof(ChartInspection), new PropertyMetadata(TimeSpan.FromMilliseconds(200)));

    /// <summary>Identifies the TransitionDuration attached property. The default is 90 milliseconds.</summary>
    public static readonly DependencyProperty TransitionDurationProperty = DependencyProperty.RegisterAttached(
        "TransitionDuration", typeof(TimeSpan), typeof(ChartInspection), new PropertyMetadata(TimeSpan.FromMilliseconds(90)));

    /// <summary>Identifies the ContentTemplate attached property.</summary>
    public static readonly DependencyProperty ContentTemplateProperty = DependencyProperty.RegisterAttached(
        "ContentTemplate", typeof(DataTemplate), typeof(ChartInspection), new PropertyMetadata(null));

    /// <summary>Gets the inspector template. Its data is a <see cref="ChartInspectionContent"/>.</summary>
    public static DataTemplate? GetContentTemplate(DependencyObject chart)
    {
        return (DataTemplate?)chart.GetValue(ContentTemplateProperty);
    }

    /// <summary>Sets the inspector template. Null uses the built-in readout.</summary>
    public static void SetContentTemplate(DependencyObject chart, DataTemplate? template)
    {
        chart.SetValue(ContentTemplateProperty, template);
    }

    /// <summary>Gets whether data inspection is enabled for the chart.</summary>
    public static bool GetIsEnabled(DependencyObject chart)
    {
        return (bool)chart.GetValue(IsEnabledProperty);
    }

    /// <summary>Sets whether data inspection is enabled for the chart.</summary>
    public static void SetIsEnabled(DependencyObject chart, bool value)
    {
        chart.SetValue(IsEnabledProperty, value);
    }

    /// <summary>Gets the delay before a chart opens its inspector on hover.</summary>
    public static TimeSpan GetHoverDelay(DependencyObject chart)
    {
        return (TimeSpan)chart.GetValue(HoverDelayProperty);
    }

    /// <summary>Sets the initial hover delay. A nonpositive delay opens the inspector immediately.</summary>
    public static void SetHoverDelay(DependencyObject chart, TimeSpan value)
    {
        chart.SetValue(HoverDelayProperty, value);
    }

    /// <summary>Gets how quickly the inspector card moves between anchors.</summary>
    public static TimeSpan GetTransitionDuration(DependencyObject chart)
    {
        return (TimeSpan)chart.GetValue(TransitionDurationProperty);
    }

    /// <summary>Sets the inspector card animation time. A nonpositive duration moves it immediately.</summary>
    public static void SetTransitionDuration(DependencyObject chart, TimeSpan value)
    {
        chart.SetValue(TransitionDurationProperty, value);
    }
}
