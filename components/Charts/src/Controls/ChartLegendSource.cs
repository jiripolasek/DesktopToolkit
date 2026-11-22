// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace JPSoftworks.DesktopToolkit.Charts;

internal readonly record struct ChartLegendEntry(
    string Name,
    Brush Fill,
    string? Value = null,
    Brush? Stroke = null,
    ChartLegendDashes? Dashes = null,
    ChartLegendSwatch? Swatch = null,
    double LineThickness = 2);

internal readonly record struct ChartLegendDashes(double Dash, double Gap)
{
    public static ChartLegendDashes Standard => new(3, 2);

    public DoubleCollection ToCollection()
    {
        return [this.Dash, this.Gap];
    }
}

internal sealed record ChartLegendScale(string Minimum, string? Maximum);

internal sealed class ChartLegendSource
{
    private event Action? Changed;

    private static readonly ConditionalWeakTable<Control, ChartLegendSource> Sources = new();
    private Action<int>? _highlight;
    private ChartLegendPresenter? _hoveredPresenter;
    private int _hoveredIndex = -1;

    public ChartLegendEntry[] Entries { get; private set; } = [];

    public ChartLegendSwatch Swatch { get; private set; }

    public ChartLegendScale? Scale { get; private set; }

    public bool HasListeners => Volatile.Read(ref this.Changed) is not null;

    public Action? RefreshFromChart { private get; set; }

    public static ChartLegendSource For(Control chart)
    {
        return Sources.GetValue(chart, static _ => new());
    }

    public void Subscribe(Action listener)
    {
        this.Changed += listener;
        (this.RefreshFromChart ?? listener)();
    }

    public void Unsubscribe(Action listener)
    {
        this.Changed -= listener;
    }

    public void Update(ChartLegendSwatch swatch, ChartLegendEntry[] entries, Action<int>? highlight = null, ChartLegendScale? scale = null)
    {
        if (this._hoveredIndex != -1 && (this._highlight != highlight || this.Entries.Length != entries.Length))
        {
            this._highlight?.Invoke(-1);
            this._hoveredPresenter = null;
            this._hoveredIndex = -1;
        }

        this.Swatch = swatch;
        this.Entries = entries;
        this.Scale = scale;
        this._highlight = highlight;
        this.Changed?.Invoke();
    }

    public void SetHovered(ChartLegendPresenter presenter, int index)
    {
        if (index >= 0 || this._hoveredPresenter == presenter)
        {
            this._hoveredPresenter = index >= 0 ? presenter : null;
            this._hoveredIndex = index;
            this._highlight?.Invoke(index);
        }
    }
}
