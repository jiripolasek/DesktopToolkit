// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.ComponentModel;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Plots supplied series on a shared numeric or time X axis.</summary>
[TemplatePart(Name = PlotPartName, Type = typeof(Canvas))]
[TemplatePart(Name = LegendPartName, Type = typeof(Grid))]
[TemplatePart(Name = DetailsPartName, Type = typeof(TextBlock))]
[TemplatePart(Name = ChartInspectorPresenter.PartName, Type = typeof(Popup))]
[WinRT.GeneratedBindableCustomProperty]
public sealed partial class LineChart : Control, INotifyPropertyChanged
{
    /// <summary>Occurs when the current X viewport changes.</summary>
    public event EventHandler? ViewportChanged;

    /// <summary>Occurs when <see cref="SelectedRange"/> changes, including after a shared range is edited or cleared.</summary>
    public event EventHandler? SelectionChanged;

    internal event EventHandler? NavigationExtentChanged;

    private const string PlotPartName = "PART_Plot";
    private const string LegendPartName = "PART_Legend";
    private const string DetailsPartName = "PART_Details";

    /// <summary>Identifies the <see cref="Data"/> dependency property.</summary>
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(LineChartData), typeof(LineChart), new PropertyMetadata(null, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="Mode"/> dependency property.</summary>
    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
        nameof(Mode), typeof(LineChartMode), typeof(LineChart), new PropertyMetadata(LineChartMode.Line, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="Interpolation"/> dependency property.</summary>
    public static readonly DependencyProperty InterpolationProperty = DependencyProperty.Register(
        nameof(Interpolation), typeof(LineChartInterpolation), typeof(LineChart),
        new PropertyMetadata(LineChartInterpolation.Linear, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="InspectionTransitionDuration"/> dependency property.</summary>
    public static readonly DependencyProperty InspectionTransitionDurationProperty = DependencyProperty.Register(
        nameof(InspectionTransitionDuration), typeof(TimeSpan), typeof(LineChart),
        new PropertyMetadata(TimeSpan.FromMilliseconds(120), OnInspectionTransitionDurationChanged));

    /// <summary>Identifies the <see cref="ShowAreaFill"/> dependency property.</summary>
    public static readonly DependencyProperty ShowAreaFillProperty = DependencyProperty.Register(
        nameof(ShowAreaFill), typeof(bool), typeof(LineChart), new PropertyMetadata(false, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="ShowGrid"/> dependency property.</summary>
    public static readonly DependencyProperty ShowGridProperty = DependencyProperty.Register(
        nameof(ShowGrid), typeof(bool), typeof(LineChart), new PropertyMetadata(true, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="ShowLegend"/> dependency property.</summary>
    public static readonly DependencyProperty ShowLegendProperty = DependencyProperty.Register(
        nameof(ShowLegend), typeof(bool), typeof(LineChart), new PropertyMetadata(true, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="XValueFormat"/> dependency property.</summary>
    public static readonly DependencyProperty XValueFormatProperty = DependencyProperty.Register(
        nameof(XValueFormat), typeof(string), typeof(LineChart), new PropertyMetadata(null, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="ValueFormat"/> dependency property.</summary>
    public static readonly DependencyProperty ValueFormatProperty = DependencyProperty.Register(
        nameof(ValueFormat), typeof(string), typeof(LineChart), new PropertyMetadata("G6", OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="XAxis"/> dependency property.</summary>
    public static readonly DependencyProperty XAxisProperty = DependencyProperty.Register(
        nameof(XAxis), typeof(ChartAxisOptions), typeof(LineChart), new PropertyMetadata(null, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="Milestones"/> dependency property.</summary>
    public static readonly DependencyProperty MilestonesProperty = DependencyProperty.Register(
        nameof(Milestones), typeof(ChartMilestones), typeof(LineChart), new PropertyMetadata(null, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="YAxis"/> dependency property.</summary>
    public static readonly DependencyProperty YAxisProperty = DependencyProperty.Register(
        nameof(YAxis), typeof(ChartAxisOptions), typeof(LineChart), new PropertyMetadata(null, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="Viewport"/> dependency property.</summary>
    public static readonly DependencyProperty ViewportProperty = DependencyProperty.Register(
        nameof(Viewport), typeof(LineChartViewport), typeof(LineChart), new PropertyMetadata(null, OnViewportChanged));

    /// <summary>Identifies the <see cref="ViewportGroup"/> dependency property.</summary>
    public static readonly DependencyProperty ViewportGroupProperty = DependencyProperty.Register(
        nameof(ViewportGroup), typeof(LineChartViewportGroup), typeof(LineChart), new PropertyMetadata(null, OnViewportGroupChanged));

    /// <summary>Identifies the <see cref="IsViewportInteractionEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty IsViewportInteractionEnabledProperty = DependencyProperty.Register(
        nameof(IsViewportInteractionEnabled), typeof(bool), typeof(LineChart), new PropertyMetadata(false, OnViewportInteractionChanged));

    /// <summary>Identifies the <see cref="IsSelectionEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty IsSelectionEnabledProperty = DependencyProperty.Register(
        nameof(IsSelectionEnabled), typeof(bool), typeof(LineChart), new PropertyMetadata(true, OnSelectionEnabledChanged));

    /// <summary>Identifies the <see cref="IsZoomEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty IsZoomEnabledProperty = DependencyProperty.Register(
        nameof(IsZoomEnabled), typeof(bool), typeof(LineChart),
        new PropertyMetadata(false, static (sender, _) => ((LineChart)sender)._presenter?.RefreshViewportInput()));

    private static readonly PropertyChangedEventArgs VisibleViewportChangedArgs = new(nameof(VisibleViewport));
    private static readonly PropertyChangedEventArgs SelectedRangeChangedArgs = new(nameof(SelectedRange));

    private readonly ChartMotionPreference _motion;

    private LineChartPresenter? _presenter;
    private LineChartViewport? _visibleViewport;
    private LineChartViewport? _sharedExtent;

    /// <summary>Gets or sets the X-axis settings. Null uses automatic settings. Numeric bounds apply to numeric data.</summary>
    public ChartAxisOptions? XAxis
    {
        get => (ChartAxisOptions?)this.GetValue(XAxisProperty);
        set => this.SetValue(XAxisProperty, value);
    }

    /// <summary>Gets or sets the milestone snapshot. Null clears the markers.</summary>
    public ChartMilestones? Milestones
    {
        get => (ChartMilestones?)this.GetValue(MilestonesProperty);
        set => this.SetValue(MilestonesProperty, value);
    }

    /// <summary>Gets or sets the Y-axis settings. Null uses automatic settings.</summary>
    public ChartAxisOptions? YAxis
    {
        get => (ChartAxisOptions?)this.GetValue(YAxisProperty);
        set => this.SetValue(YAxisProperty, value);
    }

    /// <summary>Gets or sets the requested X range. Null fits the chart or its viewport group.</summary>
    /// <remarks>Navigation changes <see cref="VisibleViewport"/> without replacing the binding on this property.</remarks>
    public LineChartViewport? Viewport
    {
        get => (LineChartViewport?)this.GetValue(ViewportProperty);
        set
        {
            if (this.Viewport == value)
            {
                this.RequestViewport(value);
            }
            else
            {
                this.SetValue(ViewportProperty, value);
            }
        }
    }

    /// <summary>Gets or sets the group that shares the chart's X window. Null makes the chart independent.</summary>
    public LineChartViewportGroup? ViewportGroup
    {
        get => (LineChartViewportGroup?)this.GetValue(ViewportGroupProperty);
        set => this.SetValue(ViewportGroupProperty, value);
    }

    /// <summary>Gets the current X viewport, or null when the chart uses its usual axis bounds independently.</summary>
    public LineChartViewport? VisibleViewport => this._visibleViewport;

    /// <summary>Gets or sets whether pointer and keyboard gestures can navigate or select a range.</summary>
    /// <remarks>Defaults to false. Programmatic navigation and an attached overview are available independently.</remarks>
    public bool IsViewportInteractionEnabled
    {
        get => (bool)this.GetValue(IsViewportInteractionEnabledProperty);
        set => this.SetValue(IsViewportInteractionEnabledProperty, value);
    }

    /// <summary>Gets or sets whether this chart supports range selection, including synchronized selections.</summary>
    /// <remarks>Defaults to true. Selecting a range on the plot also requires <see cref="IsViewportInteractionEnabled"/>.</remarks>
    public bool IsSelectionEnabled
    {
        get => (bool)this.GetValue(IsSelectionEnabledProperty);
        set => this.SetValue(IsSelectionEnabledProperty, value);
    }

    /// <summary>Gets or sets whether pointer and keyboard gestures on the plot can zoom.</summary>
    /// <remarks>Defaults to false. Zoom gestures also require <see cref="IsViewportInteractionEnabled"/>.</remarks>
    public bool IsZoomEnabled
    {
        get => (bool)this.GetValue(IsZoomEnabledProperty);
        set => this.SetValue(IsZoomEnabledProperty, value);
    }

    /// <summary>Gets the committed X selection, or null when there is no selection.</summary>
    public LineChartViewport? SelectedRange { get; private set; }

    /// <summary>Gets or sets the series snapshot. Null clears the chart.</summary>
    public LineChartData? Data
    {
        get => (LineChartData?)this.GetValue(DataProperty);
        set => this.SetValue(DataProperty, value);
    }

    /// <summary>Gets or sets the display mode. Stacked area requires matching X coordinates in every series.</summary>
    public LineChartMode Mode
    {
        get => (LineChartMode)this.GetValue(ModeProperty);
        set => this.SetValue(ModeProperty, value);
    }

    /// <summary>Gets or sets how the chart connects samples. The default is straight segments.</summary>
    public LineChartInterpolation Interpolation
    {
        get => (LineChartInterpolation)this.GetValue(InterpolationProperty);
        set => this.SetValue(InterpolationProperty, value);
    }

    /// <summary>Gets or sets the inspection guide travel time. Defaults to 120 milliseconds. Use zero for instant movement.</summary>
    public TimeSpan InspectionTransitionDuration
    {
        get => (TimeSpan)this.GetValue(InspectionTransitionDurationProperty);
        set
        {
            if (value < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            this.SetValue(InspectionTransitionDurationProperty, value);
        }
    }

    /// <summary>Gets or sets whether to fill ordinary lines to the visible zero baseline. Stacked area always fills its bands.</summary>
    public bool ShowAreaFill
    {
        get => (bool)this.GetValue(ShowAreaFillProperty);
        set => this.SetValue(ShowAreaFillProperty, value);
    }

    /// <summary>Gets or sets whether to draw grid lines.</summary>
    public bool ShowGrid
    {
        get => (bool)this.GetValue(ShowGridProperty);
        set => this.SetValue(ShowGridProperty, value);
    }

    /// <summary>Gets or sets whether to display the series legend.</summary>
    public bool ShowLegend
    {
        get => (bool)this.GetValue(ShowLegendProperty);
        set => this.SetValue(ShowLegendProperty, value);
    }

    /// <summary>Gets or sets the X value format. Time axes format timestamps in UTC.</summary>
    public string? XValueFormat
    {
        get => (string?)this.GetValue(XValueFormatProperty);
        set => this.SetValue(XValueFormatProperty, value);
    }

    /// <summary>Gets or sets the format for Y-axis labels and inspected Y values.</summary>
    public string? ValueFormat
    {
        get => (string?)this.GetValue(ValueFormatProperty);
        set => this.SetValue(ValueFormatProperty, value);
    }

    internal bool AnimationsEnabled => this._motion.AnimationsEnabled;

    internal LineChartViewport? NavigationExtent => this._sharedExtent ?? this.Data?.Extent;

    internal bool SharesSelection =>
        this.IsSelectionEnabled && this._sharedExtent is not null && this.ViewportGroup?.IsSelectionSynchronized == true;

    /// <summary>Initializes a line chart with automatic axis bounds and a series legend.</summary>
    public LineChart()
    {
        this._motion = new ChartMotionPreference(this, () => this._presenter?.FinishGuideTransition());
        this.DefaultStyleKey = typeof(LineChart);
        WinUIResourceStrings.ObserveChanges(this, () => this._presenter?.Render());
        this.RegisterPropertyChangedCallback(ChartPalette.ColorsProperty, this.OnAppearanceChanged);
        ChartAxisAppearance.RegisterCallbacks(this, this.OnAppearanceChanged);
        ChartLegend.RegisterCallbacks(this, this.OnAppearanceChanged);
        this.ActualThemeChanged += (_, _) => this._presenter?.Render();
        this.RegisterPropertyChangedCallback(ForegroundProperty, this.OnAppearanceChanged);
        this.RegisterPropertyChangedCallback(BorderBrushProperty, this.OnAppearanceChanged);
        this.RegisterPropertyChangedCallback(FontFamilyProperty, this.OnAppearanceChanged);
        this.RegisterPropertyChangedCallback(FontSizeProperty, this.OnAppearanceChanged);
    }

    /// <summary>Occurs when the visible viewport or committed selection changes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Clears the selected range and cancels a selection drag on this chart.</summary>
    /// <remarks>Clears the range on other members when this chart participates in selection synchronization.</remarks>
    public void ClearSelection()
    {
        this._presenter?.CancelSelectionDrag();
        this.SetSelectedRange(null);
    }

    /// <summary>Zooms to the selected X range and clears the selection.</summary>
    /// <returns>Whether there was a selection to zoom to.</returns>
    public bool ZoomToSelection()
    {
        if (this.SelectedRange is not { } range)
        {
            return false;
        }

        this.RequestViewport(range);
        this.ClearSelection();
        return true;
    }

    /// <summary>Fits the chart or its viewport group while preserving any binding on <see cref="Viewport"/>.</summary>
    public void ResetViewport()
    {
        if (this._visibleViewport is null)
        {
            this.ClearSelection();
        }
        else
        {
            this.RequestViewport(null);
        }
    }

    /// <summary>Zooms around a position from 0 at the left edge to 1 at the right edge.</summary>
    /// <param name="factor">A finite factor greater than zero. A factor greater than one zooms in.</param>
    /// <param name="anchor">The relative position to hold in place while zooming.</param>
    /// <returns>Whether the viewport changed.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The factor is nonfinite or nonpositive, or the anchor is nonfinite or outside [0, 1].</exception>
    public bool ZoomBy(double factor, double anchor = 0.5)
    {
        if (!double.IsFinite(factor) || factor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(factor));
        }

        if (!double.IsFinite(anchor) || anchor < 0 || anchor > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(anchor));
        }

        return factor != 1 && this.Navigate(factor, anchor, 0);
    }

    /// <summary>Pans by a fraction of the visible width. Positive values move toward later X values.</summary>
    /// <param name="fraction">The signed fraction of the current visible width to move.</param>
    /// <returns>Whether the viewport changed.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The fraction is nonfinite.</exception>
    public bool PanBy(double fraction)
    {
        if (!double.IsFinite(fraction))
        {
            throw new ArgumentOutOfRangeException(nameof(fraction));
        }

        return fraction != 0 && this.Navigate(1, 0, fraction);
    }

    /// <inheritdoc />
    [WinRT.DynamicWindowsRuntimeCast(typeof(Canvas))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(Grid))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(TextBlock))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(Popup))]
    protected override void OnApplyTemplate()
    {
        this._presenter?.Detach();
        this._presenter = null;
        base.OnApplyTemplate();

        if (this.GetTemplateChild(PlotPartName) is Canvas plot)
        {
            this._presenter = new LineChartPresenter(
                this, plot, this.GetTemplateChild(LegendPartName) as Grid, this.GetTemplateChild(DetailsPartName) as TextBlock,
                this.GetTemplateChild(ChartInspectorPresenter.PartName) as Popup);
            this._presenter.Render();
        }
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new LineChartAutomationPeer(this);
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(InputSystemCursor))]
    internal void SetViewportCursor(InputSystemCursorShape? shape)
    {
        var previous = this.ProtectedCursor as InputSystemCursor;
        if (previous?.CursorShape == shape)
        {
            return;
        }

        this.ProtectedCursor = shape is { } cursor ? InputSystemCursor.Create(cursor) : null;
        previous?.Dispose();
    }

    internal void SetSharedViewport(LineChartViewport? extent, LineChartViewport? viewport)
    {
        var previous = this._sharedExtent;
        this._sharedExtent = extent?.IsTimeBased == this.Data?.IsTimeBased ? extent : null;
        this.ApplyViewport(
            this._sharedExtent is null ? previous is null ? this.VisibleViewport : this.Viewport : viewport ?? this._sharedExtent);
        if (previous != this._sharedExtent)
        {
            this.NavigationExtentChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    internal void SetSelectedRange(LineChartViewport? range)
    {
        if (this.SharesSelection)
        {
            this.ViewportGroup!.SetSelection(range);
        }
        else
        {
            this.ApplySelectedRange(range);
        }
    }

    internal void SetSelectionPreview(LineChartViewport? range)
    {
        if (this.SharesSelection)
        {
            this.ViewportGroup!.SetSelectionPreview(this, range);
        }
        else
        {
            this.ApplySelectionPreview(range);
        }
    }

    internal void ApplySelectionPreview(LineChartViewport? range)
    {
        this._presenter?.ShowSelectionPreview(this.IsSelectionEnabled ? range : null);
    }

    internal void ApplySelectedRange(LineChartViewport? range)
    {
        range = this.IsSelectionEnabled ? range : null;
        if (this.SelectedRange != range)
        {
            this.SelectedRange = range;
            this._presenter?.RefreshSelection();
            this.PropertyChanged?.Invoke(this, SelectedRangeChangedArgs);
            this.SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    internal bool SetNavigationWindow(double first, double last)
    {
        if (this.NavigationExtent is not { } extent
            || extent.Slice(first, last) is not { } next
            || next == (this.VisibleViewport ?? extent))
        {
            return false;
        }

        this.RequestViewport(next == extent ? null : next);
        return true;
    }

    private bool Navigate(double factor, double anchor, double shift)
    {
        if (this.NavigationExtent is not { } extent)
        {
            return false;
        }

        var current = this.VisibleViewport ?? extent;
        var (first, last) = extent.GetFractions(current);
        var width = last - first;
        if (width <= 0)
        {
            return false;
        }

        var nextWidth = Math.Min(1, width / factor);
        var nextStart = Math.Clamp(first + (width - nextWidth) * anchor + width * shift, 0, 1 - nextWidth);
        return this.SetNavigationWindow(nextStart, nextStart + nextWidth);
    }

    private static void OnViewportChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((LineChart)sender).RequestViewport((LineChartViewport?)args.NewValue);
    }

    private static void OnViewportGroupChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var chart = (LineChart)sender;
        (args.OldValue as LineChartViewportGroup)?.Remove(chart);
        if (args.NewValue is LineChartViewportGroup group)
        {
            group.Add(chart);
        }
        else
        {
            chart.SetSharedViewport(null, null);
        }
    }

    private void RequestViewport(LineChartViewport? viewport)
    {
        if (this._sharedExtent is { } extent && (viewport is null || viewport.IsTimeBased == extent.IsTimeBased))
        {
            this.ViewportGroup?.RequestViewport(viewport);
        }
        else if (this._sharedExtent is null)
        {
            this.ApplyViewport(viewport);
        }
    }

    private static void OnViewportInteractionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var chart = (LineChart)sender;
        chart._presenter?.CancelGesture();
        chart.ClearSelection();
    }

    private static void OnSelectionEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var chart = (LineChart)sender;
        if (chart.IsSelectionEnabled)
        {
            chart._presenter?.RefreshViewportInput();
            if (chart.SharesSelection)
            {
                chart.ViewportGroup!.Refresh();
            }
        }
        else
        {
            chart._presenter?.CancelSelectionDrag();
            chart.ViewportGroup?.SetSelectionPreview(chart, null);
            chart.ApplySelectionPreview(null);
            chart.ApplySelectedRange(null);
        }
    }

    private void ApplyViewport(LineChartViewport? viewport)
    {
        if (viewport?.IsTimeBased != this.Data?.IsTimeBased)
        {
            viewport = null;
        }

        if (this._visibleViewport == viewport)
        {
            return;
        }

        this._visibleViewport = viewport;
        this._presenter?.CancelSelectionDrag();
        this.ApplySelectedRange(null);
        this.ApplySelectionPreview(null);
        this._presenter?.ClearInspection();
        this._presenter?.QueueViewportRender();
        this.PropertyChanged?.Invoke(this, VisibleViewportChangedArgs);
        this.ViewportChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void OnChartPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var chart = (LineChart)sender;
        var presenter = chart._presenter;
        if (args.Property == DataProperty
            || args.Property == ModeProperty
            || args.Property == XAxisProperty
            || args.Property == YAxisProperty)
        {
            presenter?.ClearInspection();
            presenter?.CancelGesture();
        }

        if (args.Property == DataProperty || args.Property == XAxisProperty)
        {
            chart.ClearSelection();
        }

        if (args.Property == DataProperty)
        {
            chart.ViewportGroup?.Refresh();
            if (chart._sharedExtent is null)
            {
                chart.ApplyViewport(chart.Viewport);
            }
        }

        presenter?.Render();
    }

    private void OnAppearanceChanged(DependencyObject sender, DependencyProperty property)
    {
        this._presenter?.Render();
    }

    private static void OnInspectionTransitionDurationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((LineChart)sender)._presenter?.FinishGuideTransition();
    }

    private sealed partial class LineChartAutomationPeer(LineChart owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore()
        {
            return nameof(LineChart);
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Group;
        }

        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrWhiteSpace(name) ? WinUIResourceStrings.Get("LineChartName") : name;
        }
    }
}
