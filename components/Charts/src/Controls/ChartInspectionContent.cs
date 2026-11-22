// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.ComponentModel;
using Microsoft.UI.Xaml.Media;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>A formatted row and its optional swatch in the data inspector.</summary>
/// <param name="Text">The formatted label or value.</param>
/// <param name="Fill">The fill brush for the swatch, when available.</param>
/// <param name="Stroke">The outline brush for the swatch, when available.</param>
public readonly record struct ChartInspectionRow(string Text, Brush? Fill = null, Brush? Stroke = null);

/// <summary>Provides formatted and structured values to a chart inspector template.</summary>
[WinRT.GeneratedBindableCustomProperty]
public sealed partial class ChartInspectionContent : INotifyPropertyChanged
{
    private static readonly PropertyChangedEventArgs AllPropertiesChanged = new(null);

    /// <summary>Gets the full formatted readout.</summary>
    public string Text { get; private set; } = string.Empty;

    /// <summary>Gets the formatted rows and optional swatches.</summary>
    public IReadOnlyList<ChartInspectionRow> Rows { get; private set; } = Array.Empty<ChartInspectionRow>();

    /// <summary>Gets the original values and associated data items, when supplied by the chart.</summary>
    public IReadOnlyList<ChartContextValue> Values { get; private set; } = Array.Empty<ChartContextValue>();

    internal ChartInspectionContent()
    {
    }

    /// <summary>Occurs when the inspector content changes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    internal void Update(string text, IReadOnlyList<ChartInspectionRow>? rows, ChartContextValue[]? values)
    {
        values ??= Array.Empty<ChartContextValue>();
        var sameRows = rows is null ? this.Rows.Count == 1 && this.Rows[0] == new ChartInspectionRow(text) : rows.SequenceEqual(this.Rows);
        if (this.Text == text && sameRows && values.SequenceEqual(this.Values))
        {
            return;
        }

        this.Text = text;
        if (!sameRows)
        {
            this.Rows = Array.AsReadOnly(rows?.ToArray() ?? [new ChartInspectionRow(text)]);
        }

        if (values.Length != 0 || this.Values.Count != 0)
        {
            this.Values = Array.AsReadOnly(values);
        }

        this.PropertyChanged?.Invoke(this, AllPropertiesChanged);
    }
}
