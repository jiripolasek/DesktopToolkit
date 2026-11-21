// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.ComponentModel;
using Microsoft.UI.Xaml.Media;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Provides the content for a chart legend entry template.</summary>
[WinRT.GeneratedBindableCustomProperty]
public sealed partial class ChartLegendItem : INotifyPropertyChanged
{
    private static readonly PropertyChangedEventArgs AllPropertiesChanged = new(null);

    /// <summary>Gets the series name or color scale label.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the formatted value, or null when the legend hides values.</summary>
    public string? ValueText { get; private set; }

    /// <summary>Gets the fill brush used by the entry's mark.</summary>
    public Brush? Fill { get; private set; }

    /// <summary>Gets the outline brush when the mark has an outline.</summary>
    public Brush? Stroke { get; private set; }

    /// <summary>Gets whether the entry represents a dashed series stroke.</summary>
    public bool IsStrokeDashed { get; private set; }

    internal ChartLegendItem()
    {
    }

    /// <summary>Occurs when a rendered entry changes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    internal void Update(string name, string? value, Brush? fill, Brush? stroke, bool isStrokeDashed = false)
    {
        if (this.Name == name
            && this.ValueText == value
            && this.Fill == fill
            && this.Stroke == stroke
            && this.IsStrokeDashed == isStrokeDashed)
        {
            return;
        }

        this.Name = name;
        this.ValueText = value;
        this.Fill = fill;
        this.Stroke = stroke;
        this.IsStrokeDashed = isStrokeDashed;
        this.PropertyChanged?.Invoke(this, AllPropertiesChanged);
    }
}
