// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>
/// Displays an animated range value or stacked capacity contributions.
/// </summary>
[TemplatePart(Name = TrackPartName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = TrackFramePartName, Type = typeof(Border))]
[TemplatePart(Name = MeterRootPartName, Type = typeof(Grid))]
[TemplatePart(Name = IndicatorHostPartName, Type = typeof(Canvas))]
[TemplatePart(Name = InactiveSegmentPartName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = GridPartName, Type = typeof(Canvas))]
[TemplatePart(Name = ValueTextPartName, Type = typeof(TextBlock))]
[TemplatePart(Name = SegmentHighlightPartName, Type = typeof(Border))]
[TemplatePart(Name = "PART_Legend", Type = typeof(ContentControl))]
[TemplatePart(Name = ChartInspectorPresenter.PartName, Type = typeof(Popup))]
[TemplateVisualState(Name = "Vertical", GroupName = "OrientationStates")]
[TemplateVisualState(Name = "Horizontal", GroupName = "OrientationStates")]
[TemplateVisualState(Name = "Standard", GroupName = "PresentationStates")]
[TemplateVisualState(Name = "Compact", GroupName = "PresentationStates")]
public sealed partial class VerticalUsageBar : RangeBase
{
    private const string TrackPartName = "PART_Track";
    private const string TrackFramePartName = "PART_TrackFrame";
    private const string MeterRootPartName = "MeterRoot";
    private const string IndicatorHostPartName = "PART_IndicatorHost";
    private const string InactiveSegmentPartName = "PART_InactiveSegment";
    private const string GridPartName = "PART_Grid";
    private const string ValueTextPartName = "PART_ValueText";
    private const string SegmentHighlightPartName = "PART_SegmentHighlight";
    private const double DefaultSegmentCornerRadius = 5;
    private const double MinimumVisibleSegmentLength = 0.01;
    private const double PixelRoundingTolerance = 0.000001;
    private static readonly TimeSpan RenderInterval = TimeSpan.FromMilliseconds(1000d / 30d);

    /// <summary>Identifies the <see cref="Orientation"/> dependency property.</summary>
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation), typeof(Orientation), typeof(VerticalUsageBar),
        new PropertyMetadata(Orientation.Vertical, OnPresentationChanged));

    /// <summary>Identifies the <see cref="IndicatorBrush"/> dependency property.</summary>
    public static readonly DependencyProperty IndicatorBrushProperty = DependencyProperty.Register(
        nameof(IndicatorBrush), typeof(Brush), typeof(VerticalUsageBar), new PropertyMetadata(null, OnVisualPropertyChanged));

    /// <summary>Identifies the <see cref="GridBrush"/> dependency property.</summary>
    public static readonly DependencyProperty GridBrushProperty = DependencyProperty.Register(
        nameof(GridBrush), typeof(Brush), typeof(VerticalUsageBar), new PropertyMetadata(null, OnGridPropertyChanged));

    /// <summary>Identifies the <see cref="GridSpacing"/> dependency property.</summary>
    public static readonly DependencyProperty GridSpacingProperty = DependencyProperty.Register(
        nameof(GridSpacing), typeof(double), typeof(VerticalUsageBar), new PropertyMetadata(12d, OnGridPropertyChanged));

    /// <summary>Identifies the <see cref="ShowGrid"/> dependency property.</summary>
    public static readonly DependencyProperty ShowGridProperty = DependencyProperty.Register(
        nameof(ShowGrid), typeof(bool), typeof(VerticalUsageBar), new PropertyMetadata(true, OnGridPropertyChanged));

    /// <summary>Identifies the <see cref="FillOpacity"/> dependency property.</summary>
    public static readonly DependencyProperty FillOpacityProperty = DependencyProperty.Register(
        nameof(FillOpacity), typeof(double), typeof(VerticalUsageBar), new PropertyMetadata(0.24d, OnVisualPropertyChanged));

    /// <summary>Identifies the <see cref="EdgeLineThickness"/> dependency property.</summary>
    public static readonly DependencyProperty EdgeLineThicknessProperty = DependencyProperty.Register(
        nameof(EdgeLineThickness), typeof(double), typeof(VerticalUsageBar), new PropertyMetadata(2d, OnVisualPropertyChanged));

    /// <summary>Identifies the <see cref="TransitionDuration"/> dependency property.</summary>
    public static readonly DependencyProperty TransitionDurationProperty = DependencyProperty.Register(
        nameof(TransitionDuration), typeof(TimeSpan), typeof(VerticalUsageBar), new PropertyMetadata(TimeSpan.FromMilliseconds(220)));

    /// <summary>Identifies the <see cref="ValueText"/> dependency property.</summary>
    public static readonly DependencyProperty ValueTextProperty = DependencyProperty.Register(
        nameof(ValueText), typeof(string), typeof(VerticalUsageBar), new PropertyMetadata(null, OnValueTextChanged));

    /// <summary>Identifies the <see cref="Data"/> dependency property.</summary>
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(VerticalUsageBarData), typeof(VerticalUsageBar), new PropertyMetadata(null, OnDataChanged));

    /// <summary>Identifies the <see cref="IsCompact"/> dependency property.</summary>
    public static readonly DependencyProperty IsCompactProperty = DependencyProperty.Register(
        nameof(IsCompact), typeof(bool), typeof(VerticalUsageBar), new PropertyMetadata(false, OnPresentationChanged));

    /// <summary>Identifies the <see cref="TrackThickness"/> dependency property.</summary>
    public static readonly DependencyProperty TrackThicknessProperty = DependencyProperty.Register(
        nameof(TrackThickness), typeof(double), typeof(VerticalUsageBar), new PropertyMetadata(double.NaN, OnPresentationChanged));

    /// <summary>Identifies the <see cref="SegmentGap"/> dependency property.</summary>
    public static readonly DependencyProperty SegmentGapProperty = DependencyProperty.Register(
        nameof(SegmentGap), typeof(double), typeof(VerticalUsageBar), new PropertyMetadata(0d, OnSegmentGapChanged));

    /// <summary>Identifies the <see cref="ShowLegend"/> dependency property.</summary>
    public static readonly DependencyProperty ShowLegendProperty = DependencyProperty.Register(
        nameof(ShowLegend), typeof(bool), typeof(VerticalUsageBar), new PropertyMetadata(false, OnLegendChanged));

    /// <summary>Identifies the <see cref="LegendOrientation"/> dependency property.</summary>
    public static readonly DependencyProperty LegendOrientationProperty = DependencyProperty.Register(
        nameof(LegendOrientation), typeof(Orientation), typeof(VerticalUsageBar),
        new PropertyMetadata(Orientation.Horizontal, OnLegendChanged));

    /// <summary>Identifies the <see cref="LegendPlacement"/> dependency property.</summary>
    public static readonly DependencyProperty LegendPlacementProperty = DependencyProperty.Register(
        nameof(LegendPlacement), typeof(UsageBarLegendPlacement), typeof(VerticalUsageBar),
        new PropertyMetadata(UsageBarLegendPlacement.End, OnLegendChanged));

    /// <summary>Identifies the <see cref="ShowLegendValues"/> dependency property.</summary>
    public static readonly DependencyProperty ShowLegendValuesProperty = DependencyProperty.Register(
        nameof(ShowLegendValues), typeof(bool), typeof(VerticalUsageBar), new PropertyMetadata(true, OnLegendChanged));

    /// <summary>Identifies the <see cref="ValueFormat"/> dependency property.</summary>
    public static readonly DependencyProperty ValueFormatProperty = DependencyProperty.Register(
        nameof(ValueFormat), typeof(string), typeof(VerticalUsageBar), new PropertyMetadata(null, OnLegendChanged));

    private readonly object _syncRoot = new();
    private readonly ChartValueDispatcher _valueDispatcher;
    private readonly List<VerticalUsageBarSeries> _series = [];
    private readonly List<(Grid Root, Border Fill)> _seriesVisuals = [];
    private readonly DispatcherTimer _animationTimer;
    private readonly ChartMotionPreference _motion;
    private readonly ChartPixelSnap _pixels;
    private readonly MonotonicClock _clock;
    private FrameworkElement? _track;
    private Grid? _meterRoot;
    private Border? _trackFrame;
    private Canvas? _indicatorHost;
    private FrameworkElement? _inactiveSegment;
    private Canvas? _gridCanvas;
    private TextBlock? _valueTextBlock;
    private Border? _segmentHighlight;
    private UsageBarLegendPresenter? _legend;
    private int _highlightedLegendSeries = -1;
    private ChartInspectorPresenter? _inspector;
    private double[] _displayedValues = [0d];
    private double[] _animationStartValues = [0d];
    private double[] _animationTargetValues = [0d];
    private DateTimeOffset _animationStartedAt;
    private TimeSpan _animationDuration;
    private bool _isAnimating;
    private bool _hasSeriesValues;
    private bool _hasBoundData;

    /// <summary>Gets or sets the snapshot of series and values. Null returns to imperative mode.</summary>
    public VerticalUsageBarData? Data
    {
        get => (VerticalUsageBarData?)this.GetValue(DataProperty);
        set => this.SetValue(DataProperty, value);
    }

    /// <summary>Gets or sets the fill direction. The default is vertical.</summary>
    public Orientation Orientation
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(Orientation))]
        get => (Orientation)this.GetValue(OrientationProperty);
        set => this.SetValue(OrientationProperty, value);
    }

    /// <summary>Gets or sets whether to show a compact track with solid segments and no grid.</summary>
    public bool IsCompact
    {
        get => (bool)this.GetValue(IsCompactProperty);
        set => this.SetValue(IsCompactProperty, value);
    }

    /// <summary>Gets or sets the track thickness in DIPs, or NaN for its presentation default.</summary>
    /// <remarks>The standard track stretches. The compact track defaults to 8 DIPs.</remarks>
    public double TrackThickness
    {
        get => (double)this.GetValue(TrackThicknessProperty);
        set
        {
            if (!double.IsNaN(value) && (!double.IsFinite(value) || value <= 0))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            this.SetValue(TrackThicknessProperty, value);
        }
    }

    /// <summary>Gets or sets the requested gap in DIPs between adjacent segments, including unused compact capacity.</summary>
    /// <remarks>Gaps use one width and shrink together to fit short segments. The end of the measured total stays in place.</remarks>
    public double SegmentGap
    {
        get => (double)this.GetValue(SegmentGapProperty);
        set
        {
            if (!double.IsFinite(value) || value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            this.SetValue(SegmentGapProperty, value);
        }
    }

    /// <summary>Gets or sets whether to show the legend for stacked contributions.</summary>
    public bool ShowLegend
    {
        get => (bool)this.GetValue(ShowLegendProperty);
        set => this.SetValue(ShowLegendProperty, value);
    }

    /// <summary>Gets or sets whether legend entries wrap in rows or stack in a column.</summary>
    public Orientation LegendOrientation
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(Orientation))]
        get => (Orientation)this.GetValue(LegendOrientationProperty);
        set => this.SetValue(LegendOrientationProperty, value);
    }

    /// <summary>Gets or sets where the legend appears relative to the track.</summary>
    public UsageBarLegendPlacement LegendPlacement
    {
        get => (UsageBarLegendPlacement)this.GetValue(LegendPlacementProperty);
        set => this.SetValue(LegendPlacementProperty, value);
    }

    /// <summary>Gets or sets whether the legend includes the raw contribution values.</summary>
    public bool ShowLegendValues
    {
        get => (bool)this.GetValue(ShowLegendValuesProperty);
        set => this.SetValue(ShowLegendValuesProperty, value);
    }

    /// <summary>Gets or sets the number format for legend and inspector values.</summary>
    public string? ValueFormat
    {
        get => (string?)this.GetValue(ValueFormatProperty);
        set => this.SetValue(ValueFormatProperty, value);
    }

    /// <summary>
    /// Gets a snapshot of the configured stacked series, in the order expected by
    /// <see cref="SetValues(double[])"/>.
    /// </summary>
    public IReadOnlyList<VerticalUsageBarSeries> Series
    {
        get
        {
            lock (this._syncRoot)
            {
                return this._series.ToArray();
            }
        }
    }

    /// <summary>
    /// Gets or sets the brush used for the translucent fill and its solid upper edge.
    /// </summary>
    public Brush? IndicatorBrush
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(Brush))]
        get => (Brush?)this.GetValue(IndicatorBrushProperty);
        set => this.SetValue(IndicatorBrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the theme-aware brush used for the cross-grid overlay.
    /// </summary>
    public Brush? GridBrush
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(Brush))]
        get => (Brush?)this.GetValue(GridBrushProperty);
        set => this.SetValue(GridBrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the grid interval in effective pixels.
    /// </summary>
    public double GridSpacing
    {
        get => (double)this.GetValue(GridSpacingProperty);
        set
        {
            if (!double.IsFinite(value) || value < 6)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "GridSpacing must be a finite value of at least 6 pixels.");
            }

            this.SetValue(GridSpacingProperty, value);
        }
    }

    /// <summary>Gets or sets whether the cross-grid overlay is visible.</summary>
    public bool ShowGrid
    {
        get => (bool)this.GetValue(ShowGridProperty);
        set => this.SetValue(ShowGridProperty, value);
    }

    /// <summary>
    /// Gets or sets the opacity of the area below the value edge.
    /// </summary>
    public double FillOpacity
    {
        get => (double)this.GetValue(FillOpacityProperty);
        set
        {
            if (!double.IsFinite(value) || value is < 0 or > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            this.SetValue(FillOpacityProperty, value);
        }
    }

    /// <summary>
    /// Gets or sets the thickness of the solid line at the current value.
    /// </summary>
    public double EdgeLineThickness
    {
        get => (double)this.GetValue(EdgeLineThicknessProperty);
        set
        {
            if (!double.IsFinite(value) || value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            this.SetValue(EdgeLineThicknessProperty, value);
        }
    }

    /// <summary>
    /// Gets or sets how long a value change takes to settle. Use zero for immediate updates.
    /// </summary>
    public TimeSpan TransitionDuration
    {
        get => (TimeSpan)this.GetValue(TransitionDurationProperty);
        set
        {
            if (value < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            this.SetValue(TransitionDurationProperty, value);
        }
    }

    /// <summary>
    /// Gets or sets optional text shown below the meter. Null or empty text hides the footer.
    /// </summary>
    public string? ValueText
    {
        get => (string?)this.GetValue(ValueTextProperty);
        set => this.SetValue(ValueTextProperty, value);
    }

    /// <summary>Initializes a new instance with the default theme and animation settings.</summary>
    public VerticalUsageBar()
        : this(TimeProvider.System)
    {
    }

    internal VerticalUsageBar(TimeProvider timeProvider)
    {
        this._clock = new MonotonicClock(timeProvider);
        this._valueDispatcher = new ChartValueDispatcher(this.DispatcherQueue, this.SetValuesCore);
        this._motion = new ChartMotionPreference(this, this.FinishAnimation);
        this._pixels = new ChartPixelSnap(this, this.UpdateTrackLayout);
        this.DefaultStyleKey = typeof(VerticalUsageBar);
        WinUIResourceStrings.ObserveChanges(
            this, () =>
            {
                this.UpdateLegend();
                this._inspector?.Refresh();
            });
        this.RegisterPropertyChangedCallback(
            CornerRadiusProperty, static (sender, _) => ((VerticalUsageBar)sender).UpdateTrackCornerRadius());
        this.RegisterPropertyChangedCallback(ForegroundProperty, static (sender, _) => ((VerticalUsageBar)sender).UpdateLegend());
        ChartLegend.RegisterCallbacks(this, static (sender, _) => ((VerticalUsageBar)sender).UpdateLegend());

        this._animationTimer = new DispatcherTimer { Interval = RenderInterval, };
        this._animationTimer.Tick += this.OnAnimationTick;

        this.Loaded += this.OnLoaded;
        this.Unloaded += this.OnUnloaded;
    }

    /// <summary>
    /// Adds a stacked visual series. Configure every series before the first call to
    /// <see cref="SetValues(double[])"/>.
    /// </summary>
    public void AddSeries(string name, Brush indicatorBrush)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(indicatorBrush);

        lock (this._syncRoot)
        {
            if (this._hasBoundData || this._hasSeriesValues)
            {
                throw new InvalidOperationException("Series cannot be added after the first multi-series value update.");
            }

            if (this._series.Any(series => string.Equals(series.Name, name, StringComparison.Ordinal)))
            {
                throw new ArgumentException($"A series named '{name}' already exists.", nameof(name));
            }

            this._series.Add(new VerticalUsageBarSeries(name, indicatorBrush));
            this._displayedValues = new double[this._series.Count];
            this._animationStartValues = new double[this._series.Count];
            this._animationTargetValues = new double[this._series.Count];
        }

        this.RebuildSeriesVisuals();
        this.UpdateLegend();
    }

    /// <summary>
    /// Updates the stacked contributions. Values use the same units as the bar's range and
    /// are clipped when their sum exceeds that range. This does not change <c>Value</c>.
    /// This method may be called from a producer thread; pending updates are coalesced so only
    /// the latest value set is applied on the UI thread.
    /// </summary>
    public void SetValues(params double[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var valuesCopy = (double[])values.Clone();

        for (var index = 0; index < valuesCopy.Length; index++)
        {
            if (!double.IsFinite(valuesCopy[index]) || valuesCopy[index] < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(values), $"Series value {index} must be finite and non-negative.");
            }
        }

        long version;
        lock (this._syncRoot)
        {
            if (this._hasBoundData)
            {
                throw new InvalidOperationException("Update the Data property when using a bound snapshot.");
            }

            if (this._series.Count == 0)
            {
                throw new InvalidOperationException("Add at least one series before setting multi-series values.");
            }

            if (valuesCopy.Length != this._series.Count)
            {
                throw new ArgumentException($"Expected {this._series.Count} values, but received {valuesCopy.Length}.", nameof(values));
            }

            this._hasSeriesValues = true;
            version = this._valueDispatcher.Version;
        }

        this._valueDispatcher.Submit(valuesCopy, version);
    }

    private static void OnDataChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var chart = (VerticalUsageBar)sender;
        var data = (VerticalUsageBarData?)args.NewValue;
        var series = data?.Series ?? Array.Empty<VerticalUsageBarSeries>();
        bool seriesChanged;
        lock (chart._syncRoot)
        {
            chart._valueDispatcher.Reset();
            chart._hasBoundData = data is not null;
            chart._hasSeriesValues = data is not null;
            seriesChanged = !chart._series.SequenceEqual(series);
            if (seriesChanged)
            {
                chart._series.Clear();
                chart._series.AddRange(series);

                chart._isAnimating = false;
                chart._displayedValues = new double[series.Count];
            }
        }

        if (seriesChanged)
        {
            chart._inspector?.Clear();
            chart._animationStartValues = chart._displayedValues;
            chart._animationTargetValues = chart._displayedValues;
            if (series.Count == 0)
            {
                chart._displayedValues = [chart.Value];
            }

            chart.RebuildSeriesVisuals();
            chart.UpdateLegend();
        }

        chart.SetValuesCore(series.Count == 0 ? [chart.Value] : data!.ValueArray);
    }

    private void SetValuesCore(double[] values)
    {
        this.BeginTransition(values, animate: this.IsLoaded);
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new VerticalUsageBarAutomationPeer(this);
    }

    /// <inheritdoc />
    [WinRT.DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(Grid))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(Border))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(Canvas))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(TextBlock))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(ContentControl))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(Popup))]
    protected override void OnApplyTemplate()
    {
        this._inspector?.Detach();
        this._inspector = null;
        if (this._track is not null)
        {
            this._track.SizeChanged -= this.OnTrackSizeChanged;
        }

        if (this._trackFrame is not null)
        {
            this._trackFrame.SizeChanged -= this.OnTrackFrameSizeChanged;
        }

        this.StopAnimation();
        base.OnApplyTemplate();

        this._track = this.GetTemplateChild(TrackPartName) as FrameworkElement;
        this._meterRoot = this.GetTemplateChild(MeterRootPartName) as Grid;
        this._trackFrame = this.GetTemplateChild(TrackFramePartName) as Border;
        this._indicatorHost = this.GetTemplateChild(IndicatorHostPartName) as Canvas;
        this._inactiveSegment = this.GetTemplateChild(InactiveSegmentPartName) as FrameworkElement;
        this._gridCanvas = this.GetTemplateChild(GridPartName) as Canvas;
        this._valueTextBlock = this.GetTemplateChild(ValueTextPartName) as TextBlock;
        this._segmentHighlight = this.GetTemplateChild(SegmentHighlightPartName) as Border;
        this._highlightedLegendSeries = -1;
        this._legend = new UsageBarLegendPresenter(
            this, this.GetTemplateChild("PART_Legend") as ContentControl, this.HighlightLegendSeries);

        if (this._track is not null)
        {
            this._track.SizeChanged += this.OnTrackSizeChanged;
            this._inspector = new ChartInspectorPresenter(
                this, this._track, this.GetTemplateChild(ChartInspectorPresenter.PartName) as Popup, () => this._seriesVisuals.Count,
                this.HitTestSegment, this.ReadSegment, this.HighlightSegment, readContext: this.ReadContext);
        }

        if (this._trackFrame is not null)
        {
            this._trackFrame.SizeChanged += this.OnTrackFrameSizeChanged;
        }

        this.UpdatePresentation();
        this.RebuildSeriesVisuals();
        this.UpdateForCurrentMode(animate: false);
        this.UpdateValueTextVisibility();
        this.RebuildGrid();
    }

    /// <inheritdoc />
    protected override void OnValueChanged(double oldValue, double newValue)
    {
        base.OnValueChanged(oldValue, newValue);
        if (this._series.Count == 0)
        {
            this.BeginTransition([newValue], animate: this.IsLoaded);
        }
    }

    /// <inheritdoc />
    protected override void OnMinimumChanged(double oldMinimum, double newMinimum)
    {
        base.OnMinimumChanged(oldMinimum, newMinimum);
        this.UpdateForCurrentMode(animate: false);
    }

    /// <inheritdoc />
    protected override void OnMaximumChanged(double oldMaximum, double newMaximum)
    {
        base.OnMaximumChanged(oldMaximum, newMaximum);
        this.UpdateForCurrentMode(animate: false);
    }

    private static void OnGridPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((VerticalUsageBar)sender).RebuildGrid();
    }

    private static void OnPresentationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var bar = (VerticalUsageBar)sender;
        bar.UpdatePresentation();
        if (args.Property != TrackThicknessProperty)
        {
            bar.RebuildSeriesVisuals();
            bar.RebuildGrid();
        }
    }

    private void UpdatePresentation()
    {
        var horizontal = this.Orientation == Orientation.Horizontal;
        VisualStateManager.GoToState(this, horizontal ? "Horizontal" : "Vertical", false);
        VisualStateManager.GoToState(this, this.IsCompact ? "Compact" : "Standard", false);
        if (this._trackFrame is not null)
        {
            var thickness = this.TrackThickness;
            thickness = double.IsFinite(thickness) && thickness > 0 ? thickness : this.IsCompact ? 8 : double.NaN;
            this._trackFrame.Width = horizontal ? double.NaN : thickness;
            this._trackFrame.Height = horizontal ? thickness : double.NaN;
            if (this._meterRoot is not null)
            {
                this._meterRoot.MinWidth = horizontal ? (this.IsCompact ? 48 : 96) : (double.IsNaN(thickness) ? 44 : 0);
                this._meterRoot.MinHeight = horizontal ? (double.IsNaN(thickness) ? 24 : 0) : (this.IsCompact ? 48 : 96);
            }
        }

        this.UpdateTrackCornerRadius();
        this.UpdateLegend();
    }

    private void OnTrackFrameSizeChanged(object sender, SizeChangedEventArgs e)
    {
        this.UpdateTrackCornerRadius();
    }

    private void UpdateTrackCornerRadius()
    {
        if (this._trackFrame is null)
        {
            return;
        }

        var radius = this.CornerRadius;
        if (this.IsCompact)
        {
            // Limit both axes together so thin tracks keep circular caps.
            var limit = Math.Min(this._trackFrame.ActualWidth, this._trackFrame.ActualHeight) / 2;
            radius = new CornerRadius(
                Math.Min(radius.TopLeft, limit), Math.Min(radius.TopRight, limit), Math.Min(radius.BottomRight, limit),
                Math.Min(radius.BottomLeft, limit));
        }

        if (!this._trackFrame.CornerRadius.Equals(radius))
        {
            this._trackFrame.CornerRadius = radius;
        }
    }

    private static void OnSegmentGapChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((VerticalUsageBar)sender).UpdateTrackLayout();
    }

    private static void OnLegendChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var bar = (VerticalUsageBar)sender;
        bar.UpdateLegend();
        if (args.Property == ValueFormatProperty)
        {
            bar._inspector?.Refresh();
        }
    }

    private void UpdateLegend()
    {
        this._legend?.Update(this._series, this._animationTargetValues);
        if (this._trackFrame is not null)
        {
            var placement = this._legend?.IsVisible == true ? UsageBarLegendPresenter.ResolvePlacement(this) : ChartLegendPlacement.Auto;
            var legacyPlacement = placement != ChartLegendPlacement.Auto && ChartLegend.GetPlacement(this) == ChartLegendPlacement.Auto;
            var leading = this.LegendPlacement == UsageBarLegendPlacement.Start;
            var horizontalPlacement = legacyPlacement ? leading ? ChartLegendPlacement.Start : ChartLegendPlacement.End : placement;
            var verticalPlacement = legacyPlacement ? leading ? ChartLegendPlacement.Top : ChartLegendPlacement.Bottom : placement;
            this._trackFrame.HorizontalAlignment = double.IsNaN(this._trackFrame.Width)
                ? HorizontalAlignment.Stretch
                : horizontalPlacement switch
                {
                    ChartLegendPlacement.Start => HorizontalAlignment.Left,
                    ChartLegendPlacement.End => HorizontalAlignment.Right,
                    _ => HorizontalAlignment.Center,
                };
            this._trackFrame.VerticalAlignment = double.IsNaN(this._trackFrame.Height)
                ? VerticalAlignment.Stretch
                : verticalPlacement switch
                {
                    ChartLegendPlacement.Top => VerticalAlignment.Top,
                    ChartLegendPlacement.Bottom => VerticalAlignment.Bottom,
                    _ => VerticalAlignment.Center,
                };
        }
    }

    private void HighlightLegendSeries(int index)
    {
        this._highlightedLegendSeries = index;
        for (var series = 0; series < this._seriesVisuals.Count; series++)
        {
            this._seriesVisuals[series].Root.Opacity = ChartLegendHighlight.OpacityFor(series, index);
        }
    }

    private static void OnVisualPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((VerticalUsageBar)sender).RebuildSeriesVisuals();
    }

    private static void OnValueTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var bar = (VerticalUsageBar)sender;
        bar.UpdateValueTextVisibility();
        bar._inspector?.Refresh();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        this.UpdateForCurrentMode(animate: false);
        this.UpdateValueTextVisibility();
        this.RebuildGrid();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        this.StopAnimation();
    }

    private void OnTrackSizeChanged(object sender, SizeChangedEventArgs e)
    {
        this.UpdateTrackLayout();
    }

    private void UpdateTrackLayout()
    {
        this.LayoutSeries(this.GetDisplayedValues(this._clock.Now));
        this.RebuildGrid();
    }

    private void UpdateForCurrentMode(bool animate)
    {
        var targets = this._series.Count == 0 ? new[] { this.Value } : this._animationTargetValues;

        this.BeginTransition(targets, animate);
    }

    private void BeginTransition(double[] targetValues, bool animate)
    {
        var now = this._clock.Now;
        var currentValues = this.GetDisplayedValues(now);
        if (currentValues.Length != targetValues.Length)
        {
            currentValues = new double[targetValues.Length];
        }

        this.StopAnimation();
        this._animationTargetValues = targetValues;
        this._legend?.Update(this._series, targetValues);

        var duration = this.TransitionDuration;
        if (!animate || !this._motion.AnimationsEnabled || duration == TimeSpan.Zero || AreClose(currentValues, targetValues))
        {
            this._displayedValues = targetValues;
            this._animationStartValues = targetValues;
            this.LayoutSeries(targetValues);
            return;
        }

        this._displayedValues = currentValues;
        this._animationStartValues = currentValues;
        this._animationStartedAt = now;
        this._animationDuration = duration;
        this._isAnimating = true;
        this.LayoutSeries(currentValues);
        this._animationTimer.Start();
    }

    private void OnAnimationTick(object? sender, object e)
    {
        var values = this.GetDisplayedValues(this._clock.Now);
        this.LayoutSeries(values);

        if (!this._isAnimating)
        {
            this._displayedValues = this._animationTargetValues;
            this._animationTimer.Stop();
        }
    }

    private void FinishAnimation()
    {
        if (this._isAnimating)
        {
            this.StopAnimation();
            this._displayedValues = this._animationTargetValues;
            this.LayoutSeries(this._displayedValues);
        }
    }

    private double[] GetDisplayedValues(DateTimeOffset now)
    {
        if (!this._isAnimating || this._animationDuration == TimeSpan.Zero)
        {
            return this._displayedValues;
        }

        var progress = Math.Clamp((now - this._animationStartedAt).TotalMilliseconds / this._animationDuration.TotalMilliseconds, 0, 1);
        var easedProgress = (1 - Math.Cos(Math.PI * progress)) / 2;
        var values = new double[this._animationTargetValues.Length];

        for (var index = 0; index < values.Length; index++)
        {
            values[index] = this._animationStartValues[index]
                            + ((this._animationTargetValues[index] - this._animationStartValues[index]) * easedProgress);
        }

        if (progress >= 1)
        {
            this._isAnimating = false;
            this._displayedValues = this._animationTargetValues;
            return this._displayedValues;
        }

        this._displayedValues = values;
        return values;
    }

    private void LayoutSeries(double[] values)
    {
        if (this._track is null || this._indicatorHost is null || this._track.ActualHeight <= 0 || this._track.ActualWidth <= 0)
        {
            return;
        }

        if (this._seriesVisuals.Count != values.Length)
        {
            this.RebuildSeriesVisuals();
            if (this._seriesVisuals.Count != values.Length)
            {
                return;
            }
        }

        var width = this._track.ActualWidth;
        var height = this._track.ActualHeight;
        var horizontal = this.Orientation == Orientation.Horizontal;
        var length = horizontal ? width : height;
        var range = this.Maximum - this.Minimum;
        if (!double.IsFinite(range) || range <= 0)
        {
            if (this._inactiveSegment is not null)
            {
                this._inactiveSegment.Visibility = Visibility.Collapsed;
            }

            this._inspector?.Refresh();
            return;
        }

        var scale = this._pixels.Scale;
        var hasSeries = this._series.Count > 0;
        var snapToPixels = this.IsCompact || hasSeries;
        var gap = this.SegmentGap;
        var gapPixels = double.IsFinite(gap) && gap > 0 ? Math.Round(Math.Min(gap, length) * scale) : 0;
        var stacked = this._series.Count > 1;
        var totalUsed = hasSeries ? Math.Min(range, values.Sum()) : Math.Clamp(values[0] - this.Minimum, 0, range);
        var usedFraction = totalUsed / range;
        var usedEdge = Snap((horizontal ? usedFraction : 1 - usedFraction) * length);
        var startBoundary = horizontal ? 0 : usedEdge;
        var endBoundary = horizontal ? usedEdge : length;
        var remaining = horizontal ? length - usedEdge : usedEdge;
        var hasInactiveGap = this.IsCompact
                             && this._inactiveSegment is not null
                             && remaining > 0
                             && endBoundary - startBoundary > MinimumVisibleSegmentLength;
        if (hasInactiveGap)
        {
            // Leave at least one pixel of inactive fill when possible.
            var availablePixels = Math.Max(0, Math.Floor(remaining * scale + PixelRoundingTolerance) - 1);
            gapPixels = Math.Min(gapPixels, availablePixels);
        }

        if (stacked && gapPixels > 0)
        {
            var measured = 0d;
            foreach (var value in values)
            {
                var (offset, limit, startsStack, endsStack) = MeasureSegment(value, ref measured);
                if (limit - offset <= MinimumVisibleSegmentLength)
                {
                    continue;
                }

                // Limit each inset to a quarter of its segment. Odd gaps put the extra pixel at the end.
                var maxInsetPixels = Math.Floor((limit - offset) * scale / 4 + PixelRoundingTolerance);
                if (!startsStack)
                {
                    gapPixels = Math.Min(gapPixels, 2 * maxInsetPixels + 1);
                }

                if (!endsStack)
                {
                    gapPixels = Math.Min(gapPixels, 2 * maxInsetPixels);
                }

                if (gapPixels == 0)
                {
                    break;
                }
            }
        }

        var startInsetPixels = Math.Floor(gapPixels / 2);
        var endInsetPixels = gapPixels - startInsetPixels;
        var cumulative = 0d;
        var crossSize = horizontal ? height : width;
        for (var index = 0; index < values.Length; index++)
        {
            var (offset, limit, startsStack, endsStack) = MeasureSegment(values[index], ref cumulative);
            if (stacked && gapPixels > 0 && limit - offset > MinimumVisibleSegmentLength)
            {
                offset += startsStack ? 0 : startInsetPixels / scale;
                limit -= endsStack ? 0 : endInsetPixels / scale;
            }

            var segmentLength = limit - offset;
            var (visual, fill) = this._seriesVisuals[index];
            if (stacked && !this.IsCompact && segmentLength > MinimumVisibleSegmentLength)
            {
                var radius = Math.Min(DefaultSegmentCornerRadius, Math.Min(segmentLength, crossSize) / 2);
                var startRadius = startsStack ? radius : 0;
                var endRadius = endsStack ? radius : 0;
                var corners = horizontal
                    ? new CornerRadius(startRadius, endRadius, endRadius, startRadius)
                    : new CornerRadius(startRadius, startRadius, endRadius, endRadius);
                if (!fill.CornerRadius.Equals(corners))
                {
                    fill.CornerRadius = corners;
                }
            }

            visual.Width = horizontal ? segmentLength : width;
            visual.Height = horizontal ? height : segmentLength;
            visual.Visibility = segmentLength > MinimumVisibleSegmentLength ? Visibility.Visible : Visibility.Collapsed;
            Canvas.SetLeft(visual, horizontal ? offset : 0);
            Canvas.SetTop(visual, horizontal ? 0 : offset);
        }

        this.LayoutInactiveSegment(width, height, horizontal, usedEdge, hasInactiveGap ? gapPixels / scale : 0);
        this._inspector?.Refresh();

        (double Offset, double Limit, bool StartsStack, bool EndsStack) MeasureSegment(double value, ref double measured)
        {
            var start = measured;
            var contribution = Math.Clamp(hasSeries ? value : value - this.Minimum, 0, range);
            measured = Math.Min(measured + contribution, range);
            var end = measured;
            var offset = Snap((horizontal ? start / range : 1 - (end / range)) * length);
            var limit = Snap((horizontal ? end / range : 1 - (start / range)) * length);
            return (offset, limit, offset <= startBoundary + MinimumVisibleSegmentLength,
                limit >= endBoundary - MinimumVisibleSegmentLength);
        }

        double Snap(double coordinate)
        {
            return snapToPixels && coordinate < length ? Math.Min(length, ChartPixelSnap.Round(coordinate, scale)) : coordinate;
        }
    }

    private void LayoutInactiveSegment(double width, double height, bool horizontal, double usedEdge, double separator)
    {
        if (this._inactiveSegment is not { } segment)
        {
            return;
        }

        var length = horizontal ? width : height;
        var remaining = horizontal ? length - usedEdge : usedEdge;
        if (!this.IsCompact || remaining <= 0)
        {
            segment.Visibility = Visibility.Collapsed;
            return;
        }

        var fillLength = remaining - separator;
        segment.Width = horizontal ? fillLength : width;
        segment.Height = horizontal ? height : fillLength;
        Canvas.SetLeft(segment, horizontal ? usedEdge + separator : 0);
        Canvas.SetTop(segment, 0);
        segment.Visibility = Visibility.Visible;
    }

    private int HitTestSegment(Point point)
    {
        if (this._track is not { } track || !new Rect(0, 0, track.ActualWidth, track.ActualHeight).Contains(point))
        {
            return -1;
        }

        if (this._series.Count == 0)
        {
            return 0;
        }

        for (var index = 0; index < this._seriesVisuals.Count; index++)
        {
            if (this.GetSegmentBounds(index) is Rect bounds && bounds.Contains(point))
            {
                return index;
            }
        }

        return -1;
    }

    private ChartInspectionItem? ReadSegment(int index)
    {
        if (this._series.Count == 0 && this._track is { } track)
        {
            var text = string.IsNullOrWhiteSpace(this.ValueText)
                ? ChartInspectorPresenter.FormatValue(this.Value, this.ValueFormat)
                : this.ValueText;
            return new ChartInspectionItem(new Point(track.ActualWidth / 2, track.ActualHeight / 2), text).WithSwatch(
                this.IndicatorBrush ?? this.Foreground);
        }

        if (this.GetSegmentBounds(index) is not Rect bounds || this._animationTargetValues[index] <= 0)
        {
            return null;
        }

        var value = ChartInspectorPresenter.FormatValue(this._animationTargetValues[index], this.ValueFormat);
        return new ChartInspectionItem(
                new Point(bounds.Right, bounds.Top + bounds.Height / 2),
                WinUIResourceStrings.FormatDefault("LabelValueFormat", this._series[index].Name, value))
            .WithSwatch(this._series[index].IndicatorBrush);
    }

    private ChartContextValue[] ReadContext(int index)
    {
        return this._series.Count == 0
            ? [new ChartContextValue(WinUIResourceStrings.Get("ValueLabel"), this.Value)]
            : [new ChartContextValue(this._series[index].Name, this._animationTargetValues[index], this._series[index], index)];
    }

    private void HighlightSegment(int index)
    {
        if (this._segmentHighlight is not { } highlight)
        {
            return;
        }

        if (index < 0 || this.GetSegmentBounds(index) is not Rect bounds)
        {
            highlight.Visibility = Visibility.Collapsed;
            return;
        }

        highlight.Width = bounds.Width;
        highlight.Height = bounds.Height;
        highlight.CornerRadius = this._seriesVisuals[index].Fill.CornerRadius;
        Canvas.SetLeft(highlight, bounds.Left);
        Canvas.SetTop(highlight, bounds.Top);
        highlight.Visibility = Visibility.Visible;
    }

    private Rect? GetSegmentBounds(int index)
    {
        var visual = this._seriesVisuals[index].Root;
        return double.IsFinite(this.Maximum - this.Minimum)
               && this.Maximum > this.Minimum
               && visual.Visibility == Visibility.Visible
               && visual.Width > 0
               && visual.Height > 0
            ? new Rect(Canvas.GetLeft(visual), Canvas.GetTop(visual), visual.Width, visual.Height)
            : null;
    }

    private void StopAnimation()
    {
        this._animationTimer.Stop();
        this._isAnimating = false;
    }

    private void RebuildSeriesVisuals()
    {
        if (this._indicatorHost is null)
        {
            return;
        }

        this._indicatorHost.Children.Clear();
        this._seriesVisuals.Clear();

        if (this._series.Count == 0)
        {
            this.AddSeriesVisual(this.IndicatorBrush ?? this.Foreground);
        }
        else
        {
            foreach (var series in this._series)
            {
                this.AddSeriesVisual(series.IndicatorBrush);
            }
        }

        this.LayoutSeries(this.GetDisplayedValues(this._clock.Now));
    }

    private void AddSeriesVisual(Brush? brush)
    {
        var horizontal = this.Orientation == Orientation.Horizontal;
        var visual = new Grid
        {
            IsHitTestVisible = false,
            Opacity = ChartLegendHighlight.OpacityFor(this._seriesVisuals.Count, this._highlightedLegendSeries),
        };
        var fill = new Border
        {
            Background = brush,
            CornerRadius = new CornerRadius(this.IsCompact ? 0 : DefaultSegmentCornerRadius),
            Opacity = this.IsCompact ? 1 : this.FillOpacity,
        };
        visual.Children.Add(fill);
        if (!this.IsCompact)
        {
            visual.Children.Add(
                new Border
                {
                    Width = horizontal ? this.EdgeLineThickness : double.NaN,
                    Height = horizontal ? double.NaN : this.EdgeLineThickness,
                    HorizontalAlignment = horizontal ? HorizontalAlignment.Right : HorizontalAlignment.Stretch,
                    VerticalAlignment = horizontal ? VerticalAlignment.Stretch : VerticalAlignment.Top,
                    Background = brush,
                    CornerRadius = new CornerRadius(1),
                });
        }

        this._seriesVisuals.Add((visual, fill));
        this._indicatorHost?.Children.Add(visual);
    }

    private void UpdateValueTextVisibility()
    {
        if (this._valueTextBlock is not null)
        {
            this._valueTextBlock.Visibility = string.IsNullOrWhiteSpace(this.ValueText) ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private static bool AreClose(double[] first, double[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }

        for (var index = 0; index < first.Length; index++)
        {
            if (Math.Abs(first[index] - second[index]) >= 0.0001)
            {
                return false;
            }
        }

        return true;
    }

    private void RebuildGrid()
    {
        if (this._gridCanvas is null || this._track is null)
        {
            return;
        }

        this._gridCanvas.Children.Clear();

        var width = this._track.ActualWidth;
        var height = this._track.ActualHeight;
        var spacing = this.GridSpacing;
        var brush = this.GridBrush ?? this.BorderBrush;

        if (this.IsCompact || !this.ShowGrid || brush is null || width <= 0 || height <= 0 || !double.IsFinite(spacing) || spacing < 6)
        {
            return;
        }

        var horizontal = this.Orientation == Orientation.Horizontal;
        var length = horizontal ? width : height;
        for (var offset = spacing; offset < length; offset += spacing)
        {
            if (horizontal)
            {
                AddGridLine(offset, 0, offset, height);
            }
            else
            {
                AddGridLine(0, height - offset, width, height - offset);
            }
        }

        if (horizontal)
        {
            AddGridLine(0, height / 2, width, height / 2);
        }
        else
        {
            AddGridLine(width / 2, 0, width / 2, height);
        }

        void AddGridLine(double x1, double y1, double x2, double y2)
        {
            this._gridCanvas.Children.Add(
                new Line
                {
                    X1 = x1,
                    X2 = x2,
                    Y1 = y1,
                    Y2 = y2,
                    Stroke = brush,
                    StrokeThickness = 1,
                    Opacity = 0.72,
                });
        }
    }

    private sealed partial class VerticalUsageBarAutomationPeer(VerticalUsageBar owner)
        : FrameworkElementAutomationPeer(owner), IRangeValueProvider
    {
        private readonly VerticalUsageBar _owner = owner;

        bool IRangeValueProvider.IsReadOnly => true;

        double IRangeValueProvider.LargeChange => this._owner.LargeChange;

        double IRangeValueProvider.Maximum => this._owner.Maximum;

        double IRangeValueProvider.Minimum => this._owner.Minimum;

        double IRangeValueProvider.SmallChange => this._owner.SmallChange;

        double IRangeValueProvider.Value => this._owner.Value;

        void IRangeValueProvider.SetValue(double value)
        {
            throw new InvalidOperationException(WinUIResourceStrings.Get("ReadOnlyUsageBar"));
        }

        protected override string GetClassNameCore()
        {
            return nameof(VerticalUsageBar);
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.ProgressBar;
        }

        protected override object? GetPatternCore(PatternInterface patternInterface)
        {
            return patternInterface == PatternInterface.RangeValue ? this : base.GetPatternCore(patternInterface);
        }
    }
}
