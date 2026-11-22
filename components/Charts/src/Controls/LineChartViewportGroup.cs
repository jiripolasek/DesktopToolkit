// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Shares the visible X range among line charts on the same numeric or time axis.</summary>
/// <remarks>Assign one instance to the charts' <see cref="LineChart.ViewportGroup"/> properties on the UI thread.</remarks>
public sealed partial class LineChartViewportGroup : DependencyObject
{
    /// <summary>Identifies the <see cref="IsSelectionSynchronized"/> dependency property.</summary>
    public static readonly DependencyProperty IsSelectionSynchronizedProperty = DependencyProperty.Register(
        nameof(IsSelectionSynchronized), typeof(bool), typeof(LineChartViewportGroup),
        new PropertyMetadata(false, static (sender, _) => ((LineChartViewportGroup)sender).OnSelectionSyncChanged()));

    private readonly List<WeakReference<LineChart>> _members = [];
    private LineChartViewport? _requestedViewport;
    private LineChartViewport? _effectiveViewport;
    private LineChartViewport? _selection;
    private LineChartViewport? _preview;
    private WeakReference<LineChart>? _previewOwner;
    private bool _hasRequest;
    private bool _isRefreshing;
    private bool _refreshPending;

    /// <summary>Gets or sets whether compatible member charts share drag previews and the selected X range.</summary>
    /// <remarks>Defaults to false. When enabled, a viewport change clears the shared range.</remarks>
    public bool IsSelectionSynchronized
    {
        get => (bool)this.GetValue(IsSelectionSynchronizedProperty);
        set => this.SetValue(IsSelectionSynchronizedProperty, value);
    }

    /// <summary>Creates a group that fits its members' data until a viewport is requested.</summary>
    public LineChartViewportGroup()
    {
    }

    internal void Add(LineChart chart)
    {
        this._members.Add(new WeakReference<LineChart>(chart));
        this.Refresh();
    }

    internal void Remove(LineChart chart)
    {
        this.SetSelectionPreview(chart, null);
        this._members.RemoveAll(member => !member.TryGetTarget(out var target) || target == chart);
        this.Refresh();
    }

    internal void RequestViewport(LineChartViewport? viewport)
    {
        this._hasRequest = true;
        this._requestedViewport = viewport;
        this.Refresh();
    }

    internal void SetSelection(LineChartViewport? selection)
    {
        if (this._selection == selection)
        {
            return;
        }

        this._selection = selection;
        this.Refresh();
    }

    internal void SetSelectionPreview(LineChart chart, LineChartViewport? preview)
    {
        var hasOwner = this._previewOwner is not null && this._previewOwner.TryGetTarget(out var owner) && owner == chart;
        if ((preview is null && !hasOwner) || (hasOwner && this._preview == preview))
        {
            return;
        }

        this._previewOwner = preview is null ? null : hasOwner ? this._previewOwner : new WeakReference<LineChart>(chart);
        this._preview = preview;
        foreach (var member in this._members)
        {
            if (member.TryGetTarget(out var target) && target.ViewportGroup == this && target.SharesSelection)
            {
                target.ApplySelectionPreview(preview);
            }
        }
    }

    private void OnSelectionSyncChanged()
    {
        this._selection = null;
        this._preview = null;
        this._previewOwner = null;
        foreach (var member in this._members)
        {
            if (member.TryGetTarget(out var chart))
            {
                chart.ApplySelectionPreview(null);
            }
        }

        this.Refresh();
    }

    internal void Refresh()
    {
        this._refreshPending = true;
        if (this._isRefreshing)
        {
            return;
        }

        this._isRefreshing = true;
        try
        {
            while (this._refreshPending)
            {
                this._refreshPending = false;
                this.UpdateMembers();
            }
        }
        finally
        {
            this._isRefreshing = false;
        }
    }

    private void UpdateMembers()
    {
        this._members.RemoveAll(static member => !member.TryGetTarget(out _));
        var charts = new List<LineChart>(this._members.Count);
        LineChartViewport? extent = null;
        foreach (var member in this._members)
        {
            if (member.TryGetTarget(out var chart))
            {
                charts.Add(chart);
                if (chart.Data?.Extent is { } bounds)
                {
                    extent = extent is null ? bounds : extent.Union(bounds);
                }
            }
        }

        if (!this._hasRequest && extent is not null)
        {
            foreach (var chart in charts)
            {
                if (chart.Data?.IsTimeBased == extent.IsTimeBased
                    && chart.Viewport is { } request
                    && request.IsTimeBased == extent.IsTimeBased)
                {
                    this._requestedViewport = request;
                    this._hasRequest = true;
                    break;
                }
            }
        }

        var viewport = this._requestedViewport?.IsTimeBased == extent?.IsTimeBased ? this._requestedViewport : null;
        if (this._effectiveViewport != (viewport ?? extent))
        {
            this._effectiveViewport = viewport ?? extent;
            this._selection = this._preview = null;
            this._previewOwner = null;
        }

        foreach (var chart in charts)
        {
            if (this._refreshPending)
            {
                break;
            }

            if (chart.ViewportGroup == this)
            {
                chart.SetSharedViewport(extent, viewport);
                if (!this._refreshPending && chart.SharesSelection)
                {
                    chart.ApplySelectedRange(this._selection);
                    chart.ApplySelectionPreview(this._preview);
                }
            }
        }
    }
}
