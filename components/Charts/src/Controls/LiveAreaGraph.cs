// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Numerics;
using System.Text;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>
/// Displays continuously scrolling time-series areas with optional pointer and keyboard inspection.
/// </summary>
[TemplatePart(Name = CanvasHostPartName, Type = typeof(Grid))]
[TemplatePart(Name = TooltipPartName, Type = typeof(Border))]
[TemplatePart(Name = TooltipTimePartName, Type = typeof(TextBlock))]
[TemplatePart(Name = TooltipValuesPartName, Type = typeof(StackPanel))]
[TemplateVisualState(Name = "Standard", GroupName = "PresentationStates")]
[TemplateVisualState(Name = "Sparkline", GroupName = "PresentationStates")]
public sealed partial class LiveAreaGraph : Control
{
    private const string CanvasHostPartName = "PART_CanvasHost";
    private const string TooltipPartName = "PART_Tooltip";
    private const string TooltipTimePartName = "PART_TooltipTime";
    private const string TooltipValuesPartName = "PART_TooltipValues";
    private const string DefaultTooltipValueFormat = "0.#";
    private const double DefaultMaximum = 100;
    private const float PointOutlineRadius = 4.25f;
    private const float PointFillRadius = 2.75f;
    private static readonly TimeSpan RenderInterval = TimeSpan.FromMilliseconds(1000d / 30d);
    private static readonly TimeSpan InspectionInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>Identifies the <see cref="IsTooltipEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty IsTooltipEnabledProperty = DependencyProperty.Register(
        nameof(IsTooltipEnabled), typeof(bool), typeof(LiveAreaGraph), new PropertyMetadata(true, OnTooltipEnabledChanged));

    /// <summary>Identifies the <see cref="TooltipValueFormat"/> dependency property.</summary>
    public static readonly DependencyProperty TooltipValueFormatProperty = DependencyProperty.Register(
        nameof(TooltipValueFormat), typeof(string), typeof(LiveAreaGraph),
        new PropertyMetadata(DefaultTooltipValueFormat, OnTooltipPresentationChanged));

    /// <summary>Identifies the <see cref="TooltipValueSuffix"/> dependency property.</summary>
    public static readonly DependencyProperty TooltipValueSuffixProperty = DependencyProperty.Register(
        nameof(TooltipValueSuffix), typeof(string), typeof(LiveAreaGraph),
        new PropertyMetadata(string.Empty, OnTooltipPresentationChanged));

    /// <summary>Identifies the <see cref="IsSparkline"/> dependency property.</summary>
    public static readonly DependencyProperty IsSparklineProperty = DependencyProperty.Register(
        nameof(IsSparkline), typeof(bool), typeof(LiveAreaGraph), new PropertyMetadata(false, OnPlotPresentationChanged));

    /// <summary>Identifies the <see cref="ShowAreaFill"/> dependency property.</summary>
    public static readonly DependencyProperty ShowAreaFillProperty = DependencyProperty.Register(
        nameof(ShowAreaFill), typeof(bool), typeof(LiveAreaGraph), new PropertyMetadata(true, OnPlotPresentationChanged));

    /// <summary>Identifies the <see cref="ShowLatestValueMarker"/> dependency property.</summary>
    public static readonly DependencyProperty ShowLatestValueMarkerProperty = DependencyProperty.Register(
        nameof(ShowLatestValueMarker), typeof(bool), typeof(LiveAreaGraph), new PropertyMetadata(false, OnPlotPresentationChanged));

    /// <summary>Identifies the <see cref="Interpolation"/> dependency property.</summary>
    public static readonly DependencyProperty InterpolationProperty = DependencyProperty.Register(
        nameof(Interpolation), typeof(LiveGraphInterpolation), typeof(LiveAreaGraph),
        new PropertyMetadata(LiveGraphInterpolation.Eased, OnInterpolationChanged));

    /// <summary>Identifies the <see cref="HistoryDuration"/> dependency property.</summary>
    public static readonly DependencyProperty HistoryDurationProperty = DependencyProperty.Register(
        nameof(HistoryDuration), typeof(TimeSpan), typeof(LiveAreaGraph),
        new PropertyMetadata(
            TimeSpan.FromSeconds(60), static (sender, args) => ((LiveAreaGraph)sender).SetHistoryDurationCore((TimeSpan)args.NewValue)));

    /// <summary>Identifies the <see cref="GridMode"/> dependency property.</summary>
    public static readonly DependencyProperty GridModeProperty = DependencyProperty.Register(
        nameof(GridMode), typeof(LiveGraphGridMode), typeof(LiveAreaGraph),
        new PropertyMetadata(
            LiveGraphGridMode.Cross, static (sender, args) => ((LiveAreaGraph)sender).SetGridModeCore((LiveGraphGridMode)args.NewValue)));

    /// <summary>Identifies the <see cref="TransitionDuration"/> dependency property.</summary>
    public static readonly DependencyProperty TransitionDurationProperty = DependencyProperty.Register(
        nameof(TransitionDuration), typeof(TimeSpan), typeof(LiveAreaGraph),
        new PropertyMetadata(
            TimeSpan.FromMilliseconds(420),
            static (sender, args) => ((LiveAreaGraph)sender).SetTransitionDurationCore((TimeSpan)args.NewValue)));

    /// <summary>Identifies the <see cref="GridSpacing"/> dependency property.</summary>
    public static readonly DependencyProperty GridSpacingProperty = DependencyProperty.Register(
        nameof(GridSpacing), typeof(double), typeof(LiveAreaGraph),
        new PropertyMetadata(32d, static (sender, args) => ((LiveAreaGraph)sender).SetGridSpacingCore((double)args.NewValue)));

    /// <summary>Identifies the <see cref="IsPaused"/> dependency property.</summary>
    public static readonly DependencyProperty IsPausedProperty = DependencyProperty.Register(
        nameof(IsPaused), typeof(bool), typeof(LiveAreaGraph),
        new PropertyMetadata(false, static (sender, args) => ((LiveAreaGraph)sender).SetIsPausedCore((bool)args.NewValue)));

    /// <summary>Identifies the <see cref="ValueRange"/> dependency property.</summary>
    public static readonly DependencyProperty ValueRangeProperty = DependencyProperty.Register(
        nameof(ValueRange), typeof(LiveGraphRange), typeof(LiveAreaGraph),
        new PropertyMetadata(null, static (sender, args) => ((LiveAreaGraph)sender).ApplyValueRange((LiveGraphRange?)args.NewValue)));

    /// <summary>Identifies the <see cref="SeriesSource"/> dependency property.</summary>
    public static readonly DependencyProperty SeriesSourceProperty = DependencyProperty.Register(
        nameof(SeriesSource), typeof(LiveGraphSeriesCollection), typeof(LiveAreaGraph),
        new PropertyMetadata(
            null, static (sender, args) => ((LiveAreaGraph)sender).ApplySeriesSource((LiveGraphSeriesCollection?)args.NewValue)));

    private readonly object _syncRoot = new();
    private readonly MonotonicClock _clock;
    private readonly List<GraphSample> _samples = [];
    private readonly List<TextBlock> _tooltipValueTexts = [];
    private readonly DispatcherTimer _inspectionTimer;
    private readonly ChartLegendSource _legendSource;
    private readonly DispatcherQueue _uiDispatcher;
    private LiveGraphSeries[]? _legendSeries;
    private volatile bool _legendShowsValues;
    private int _legendUpdateQueued;
    private LiveGraphSeries[] _seriesSnapshot = [];
    private IReadOnlyList<LiveGraphSeries> _seriesView = Array.Empty<LiveGraphSeries>();
    private bool _usesSeriesSource;
    private Brush[] _tooltipSwatches = [];
    private LiveGraphSeries[]? _tooltipSeries;
    private ContentPresenter? _customTooltipPresenter;
    private ChartInspectionContent? _customTooltipContent;
    private DispatcherQueueTimer? _hoverTimer;
    private Point _pendingInspection;
    private ChartContextMenuPresenter? _contextMenu;
    private ChartInspectorMotion? _tooltipMotion;
    private Grid? _canvasHost;
    private CanvasAnimatedControl? _canvas;
    private Border? _tooltipBorder;
    private TextBlock? _tooltipTimeText;
    private StackPanel? _tooltipValuesPanel;
    private CanvasStrokeStyle? _dashedEdgeLineStyle;
    private TimeSpan _historyDuration = TimeSpan.FromSeconds(60);
    private TimeSpan _transitionDuration = TimeSpan.FromMilliseconds(420);
    private LiveGraphInterpolation _interpolation;
    private LiveGraphGridMode _gridMode = LiveGraphGridMode.Cross;
    private bool _isSparkline;
    private bool _showAreaFill = true;
    private bool _showLatestValueMarker;
    private double _gridSpacing = 32;
    private double _minimum = 0;
    private double _maximum = DefaultMaximum;
    private Color _effectiveGridColor;
    private Color _effectiveInspectionLineColor;
    private Color _effectiveInspectionPointOutlineColor;
    private bool _isPaused;
    private DateTimeOffset _pausedAt;
    private bool _isInspectionVisible;
    private bool _isPointerOver;
    private bool _isKeyboardInspecting;
    private double _inspectionX;
    private double _inspectionY;

    /// <summary>
    /// Gets a snapshot of the configured series, in the same order expected by
    /// <see cref="AppendSample(double[])"/>.
    /// </summary>
    public IReadOnlyList<LiveGraphSeries> Series
    {
        get
        {
            lock (this._syncRoot)
            {
                return this._seriesView;
            }
        }
    }

    /// <summary>
    /// Gets or sets whether pointer and keyboard inspection is available.
    /// </summary>
    public bool IsTooltipEnabled
    {
        get => (bool)this.GetValue(IsTooltipEnabledProperty);
        set => this.SetValue(IsTooltipEnabledProperty, value);
    }

    /// <summary>
    /// Gets or sets the numeric format used for values in the inspection tooltip.
    /// </summary>
    public string TooltipValueFormat
    {
        get => (string?)this.GetValue(TooltipValueFormatProperty) ?? DefaultTooltipValueFormat;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            this.SetValue(TooltipValueFormatProperty, value);
        }
    }

    /// <summary>
    /// Gets or sets text appended to each formatted inspection value, such as "%".
    /// </summary>
    public string TooltipValueSuffix
    {
        get => (string?)this.GetValue(TooltipValueSuffixProperty) ?? string.Empty;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            this.SetValue(TooltipValueSuffixProperty, value);
        }
    }

    /// <summary>Gets or sets whether the graph uses compact sizing and hides the grid.</summary>
    public bool IsSparkline
    {
        get => (bool)this.GetValue(IsSparklineProperty);
        set => this.SetValue(IsSparklineProperty, value);
    }

    /// <summary>Gets or sets whether to fill the area beneath each series. Defaults to true.</summary>
    public bool ShowAreaFill
    {
        get => (bool)this.GetValue(ShowAreaFillProperty);
        set => this.SetValue(ShowAreaFillProperty, value);
    }

    /// <summary>Gets or sets whether to mark each series' current rendered value at the right edge.</summary>
    public bool ShowLatestValueMarker
    {
        get => (bool)this.GetValue(ShowLatestValueMarkerProperty);
        set => this.SetValue(ShowLatestValueMarkerProperty, value);
    }

    /// <summary>Gets or sets how incoming values change the curve. Changing the mode redraws retained history.</summary>
    public LiveGraphInterpolation Interpolation
    {
        get => (LiveGraphInterpolation)this.GetValue(InterpolationProperty);
        set => this.SetValue(InterpolationProperty, value);
    }

    /// <summary>
    /// Gets or sets the amount of time visible in the graph.
    /// </summary>
    public TimeSpan HistoryDuration
    {
        get
        {
            lock (this._syncRoot)
            {
                return this._historyDuration;
            }
        }

        set => this.SetValue(HistoryDurationProperty, value);
    }

    /// <summary>Gets or sets the background guide pattern.</summary>
    public LiveGraphGridMode GridMode
    {
        get
        {
            lock (this._syncRoot)
            {
                return this._gridMode;
            }
        }

        set => this.SetValue(GridModeProperty, value);
    }

    /// <summary>
    /// Gets or sets how long a newly appended endpoint takes to reach its sampled value.
    /// Set this to <see cref="TimeSpan.Zero"/> for immediate, step-like updates.
    /// Smooth mode can extend a nonzero duration to cover the recent cadence, up to twice the configured duration.
    /// </summary>
    public TimeSpan TransitionDuration
    {
        get
        {
            lock (this._syncRoot)
            {
                return this._transitionDuration;
            }
        }

        set => this.SetValue(TransitionDurationProperty, value);
    }

    /// <summary>
    /// Gets or sets the grid interval in effective pixels.
    /// </summary>
    public double GridSpacing
    {
        get
        {
            lock (this._syncRoot)
            {
                return this._gridSpacing;
            }
        }

        set => this.SetValue(GridSpacingProperty, value);
    }

    /// <summary>Gets or sets the finite lower bound, which must be less than Maximum.</summary>
    public double Minimum
    {
        get
        {
            lock (this._syncRoot)
            {
                return this._minimum;
            }
        }

        set
        {
            var other = this.Maximum;
            if (!double.IsFinite(value) || value >= other)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Minimum must be finite and lower than Maximum.");
            }

            this.ValueRange = new LiveGraphRange(value, other);
        }
    }

    /// <summary>Gets or sets the finite upper bound, which must be greater than Minimum.</summary>
    public double Maximum
    {
        get
        {
            lock (this._syncRoot)
            {
                return this._maximum;
            }
        }

        set
        {
            var other = this.Minimum;
            if (!double.IsFinite(value) || value <= other)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Maximum must be finite and higher than Minimum.");
            }

            this.ValueRange = new LiveGraphRange(other, value);
        }
    }

    /// <summary>
    /// Freezes the graph at its current position. Samples cannot be appended while paused.
    /// Resuming shifts the retained timestamps so the visible graph continues where it stopped.
    /// </summary>
    public bool IsPaused
    {
        get
        {
            lock (this._syncRoot)
            {
                return this._isPaused;
            }
        }

        set => this.SetValue(IsPausedProperty, value);
    }

    /// <summary>Gets or sets the bindable series configuration.</summary>
    /// <remarks>
    /// The order defines the sample columns. Replacing only their appearance retains history.
    /// Changing the names or order clears history. Null clears the graph and restores imperative setup.
    /// </remarks>
    public LiveGraphSeriesCollection? SeriesSource
    {
        get => (LiveGraphSeriesCollection?)this.GetValue(SeriesSourceProperty);
        set => this.SetValue(SeriesSourceProperty, value);
    }

    /// <summary>Gets or sets both vertical bounds. Null restores the default range of 0 to 100.</summary>
    public LiveGraphRange? ValueRange
    {
        get => (LiveGraphRange?)this.GetValue(ValueRangeProperty);
        set => this.SetValue(ValueRangeProperty, value);
    }

    private bool CanInspect =>
        this.IsEnabled && this.IsTooltipEnabled && this._contextMenu?.IsOpen != true && ChartInspection.GetIsEnabled(this);

    /// <summary>Initializes a new instance with the default theme and animation settings.</summary>
    public LiveAreaGraph()
        : this(TimeProvider.System)
    {
    }

    internal LiveAreaGraph(TimeProvider timeProvider)
    {
        this._clock = new MonotonicClock(timeProvider);
        this._uiDispatcher = this.DispatcherQueue;
        this._legendSource = ChartLegendSource.For(this);
        this._legendSource.RefreshFromChart = this.RefreshLegend;
        WinUIResourceStrings.ObserveChanges(
            this, () =>
            {
                this.RefreshLegend();
                this.UpdateInspectionTooltip();
            });
        ChartLegend.RegisterCallbacks(this, OnLegendAppearanceChanged);
        this.RegisterPropertyChangedCallback(ForegroundProperty, OnLegendAppearanceChanged);
        this.RegisterPropertyChangedCallback(FontSizeProperty, OnLegendAppearanceChanged);
        this.RegisterPropertyChangedCallback(FontFamilyProperty, OnLegendAppearanceChanged);
        this.DefaultStyleKey = typeof(LiveAreaGraph);
        this.IsTabStop = true;
        this.UseSystemFocusVisuals = true;

        this._inspectionTimer = new DispatcherTimer { Interval = InspectionInterval, };
        this._inspectionTimer.Tick += this.OnInspectionTimerTick;

        this.Loaded += this.OnLoaded;
        this.Unloaded += this.OnUnloaded;
        this.ActualThemeChanged += this.OnActualThemeChanged;
        this.PointerEntered += this.OnPointerEnteredOrMoved;
        this.PointerMoved += this.OnPointerEnteredOrMoved;
        this.PointerExited += this.OnPointerExited;
        this.RegisterPropertyChangedCallback(
            ChartInspection.IsEnabledProperty, (_, _) =>
            {
                if (!ChartInspection.GetIsEnabled(this))
                {
                    this.HideInspection();
                }
            });
        this.RegisterPropertyChangedCallback(
            ChartInspection.ContentTemplateProperty, (_, _) =>
            {
                this.ResetTooltipRows();
                this.UpdateInspectionTooltip();
            });
        this.IsEnabledChanged += (_, _) =>
        {
            if (!this.IsEnabled)
            {
                this.HideInspection();
            }
        };
        this.GotFocus += this.OnGotFocus;
        this.LostFocus += this.OnLostFocus;
        this.KeyDown += this.OnKeyDown;

        this.UpdateEffectiveGridColor();
    }

    private void ApplySeriesSource(LiveGraphSeriesCollection? source)
    {
        var next = source?.Snapshot ?? Array.Empty<LiveGraphSeries>();
        lock (this._syncRoot)
        {
            var sameColumns = this._seriesSnapshot.Length == next.Length;
            for (var index = 0; sameColumns && index < next.Length; index++)
            {
                sameColumns = string.Equals(this._seriesSnapshot[index].Name, next[index].Name, StringComparison.Ordinal);
            }

            if (!sameColumns)
            {
                this._samples.Clear();
            }

            this._seriesSnapshot = next;
            this._seriesView = source?.Series ?? Array.Empty<LiveGraphSeries>();
            this._usesSeriesSource = source is not null;
        }

        this.HideInspection();
        this.ResetTooltipRows();
        this.RefreshLegend();
    }

    private void ApplyValueRange(LiveGraphRange? range)
    {
        lock (this._syncRoot)
        {
            this._minimum = range?.Minimum ?? 0;
            this._maximum = range?.Maximum ?? DefaultMaximum;
        }

        this._canvas?.Invalidate();
    }

    private void SetHistoryDurationCore(TimeSpan value)
    {
        if (value < TimeSpan.FromSeconds(1))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "HistoryDuration must be at least one second.");
        }

        lock (this._syncRoot)
        {
            this._historyDuration = value;
            this.TrimSamplesCore(this._samples.Count > 0 ? this._samples[^1].Timestamp : this._clock.Now);
        }

        this._canvas?.Invalidate();
    }

    private void SetGridModeCore(LiveGraphGridMode value)
    {
        lock (this._syncRoot)
        {
            this._gridMode = value;
        }

        this._canvas?.Invalidate();
    }

    private void SetTransitionDurationCore(TimeSpan value)
    {
        if (value < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        lock (this._syncRoot)
        {
            this._transitionDuration = value;
        }
    }

    private void SetGridSpacingCore(double value)
    {
        if (!double.IsFinite(value) || value < 8)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "GridSpacing must be a finite value of at least 8 pixels.");
        }

        lock (this._syncRoot)
        {
            this._gridSpacing = value;
        }

        this._canvas?.Invalidate();
    }

    private void SetIsPausedCore(bool value)
    {
        CanvasAnimatedControl? canvas;

        lock (this._syncRoot)
        {
            if (this._isPaused == value)
            {
                return;
            }

            if (value)
            {
                this._pausedAt = this._clock.Now;
            }
            else
            {
                var pausedDuration = this._clock.Now - this._pausedAt;
                for (var index = 0; index < this._samples.Count; index++)
                {
                    this._samples[index] = this._samples[index] with { Timestamp = this._samples[index].Timestamp + pausedDuration, };
                }
            }

            this._isPaused = value;
            canvas = this._canvas;
        }

        if (canvas is not null)
        {
            canvas.Paused = value;
        }
    }

    /// <summary>
    /// Adds a series and returns its immutable descriptor. Configure every series before
    /// appending the first sample so each sample has a stable value-to-series mapping.
    /// </summary>
    public LiveGraphSeries AddSeries(
        string name,
        Color edgeLineColor,
        Color fillColor,
        float edgeLineThickness = 2f,
        LiveGraphEdgeLineStyle edgeLineStyle = LiveGraphEdgeLineStyle.Solid)
    {
        var added = new LiveGraphSeries(name, edgeLineColor, fillColor, edgeLineThickness, edgeLineStyle);
        lock (this._syncRoot)
        {
            if (this._usesSeriesSource)
            {
                throw new InvalidOperationException("Use SeriesSource to configure series when it is set.");
            }

            if (this._samples.Count != 0)
            {
                throw new InvalidOperationException("All graph series must be configured before the first sample is appended.");
            }

            if (this._seriesSnapshot.Any(series => string.Equals(series.Name, name, StringComparison.Ordinal)))
            {
                throw new ArgumentException($"A graph series named '{name}' already exists.", nameof(name));
            }

            this._seriesSnapshot = [.. this._seriesSnapshot, added];
            this._seriesView = Array.AsReadOnly(this._seriesSnapshot);
        }

        this.QueueLegendUpdate();
        return added;
    }

    /// <summary>
    /// Appends one timestamped value for every configured series. This method is safe to
    /// call from a producer thread; sample updates and rendering synchronize through a short lock.
    /// </summary>
    public void AppendSample(params double[] values)
    {
        this.AppendSample(this._clock.Now, values);
    }

    /// <summary>
    /// Appends one timestamped value for every configured series.
    /// </summary>
    public void AppendSample(DateTimeOffset timestamp, params double[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        lock (this._syncRoot)
        {
            if (this._isPaused)
            {
                throw new InvalidOperationException("Resume the graph before appending samples.");
            }

            if (this._seriesSnapshot.Length == 0)
            {
                throw new InvalidOperationException("Add at least one graph series before appending samples.");
            }

            if (values.Length != this._seriesSnapshot.Length)
            {
                throw new ArgumentException(
                    $"Expected {this._seriesSnapshot.Length} values, but received {values.Length}.", nameof(values));
            }

            if (values.Any(value => !double.IsFinite(value)))
            {
                throw new ArgumentException("Graph samples must contain only finite values.", nameof(values));
            }

            if (this._samples.Count > 0 && timestamp < this._samples[^1].Timestamp)
            {
                throw new ArgumentOutOfRangeException(nameof(timestamp), "Sample timestamps must be monotonic.");
            }

            var previous = this._samples.Count > 0 ? this._samples[^1] : null;
            this._samples.Add(this.CreateSample(previous, timestamp, (double[])values.Clone(), this._transitionDuration));
            this.TrimSamplesCore(timestamp);
        }

        if (this._legendShowsValues)
        {
            this.QueueLegendUpdate();
        }
    }

    /// <summary>Removes retained samples while preserving the configured series and pause state.</summary>
    public void Clear()
    {
        lock (this._syncRoot)
        {
            this._samples.Clear();
        }

        this.QueueLegendUpdate();
        this._canvas?.Invalidate();
    }

    private void QueueLegendUpdate()
    {
        if (!this._legendSource.HasListeners || Interlocked.Exchange(ref this._legendUpdateQueued, 1) != 0)
        {
            return;
        }

        if (!this._uiDispatcher.TryEnqueue(() =>
            {
                Volatile.Write(ref this._legendUpdateQueued, 0);
                this.RefreshLegend();
            }))
        {
            Volatile.Write(ref this._legendUpdateQueued, 0);
        }
    }

    private static void OnLegendAppearanceChanged(DependencyObject sender, DependencyProperty property)
    {
        ((LiveAreaGraph)sender).RefreshLegend();
    }

    private void RefreshLegend()
    {
        if (!this._legendSource.HasListeners)
        {
            return;
        }

        this._legendShowsValues = ChartLegend.GetValueMode(this) == ChartLegendValueMode.Value;
        LiveGraphSeries[] series;
        double[]? values;
        lock (this._syncRoot)
        {
            series = this._seriesSnapshot;
            values = this._legendShowsValues && this._samples.Count > 0 ? this._samples[^1].TargetValues : null;
        }

        var entries = this._legendSource.Entries;
        if (this._legendSeries != series)
        {
            this._legendSeries = series;
            entries = new ChartLegendEntry[series.Length];
            for (var index = 0; index < series.Length; index++)
            {
                var item = series[index];
                var fillOnly = item.EdgeLineStyle == LiveGraphEdgeLineStyle.None;
                // Square caps on the Win2D stroke produce a three-to-one dash pattern.
                entries[index] = new(
                    item.Name, new SolidColorBrush(fillOnly ? item.FillColor : item.EdgeLineColor),
                    Dashes: item.EdgeLineStyle == LiveGraphEdgeLineStyle.Dashed ? new ChartLegendDashes(3, 1) : null,
                    Swatch: fillOnly ? ChartLegendSwatch.Box : ChartLegendSwatch.Line, LineThickness: item.EdgeLineThickness);
            }
        }

        for (var index = 0; index < entries.Length; index++)
        {
            entries[index] = entries[index] with { Value = values is null ? null : this.FormatTooltipValue(values[index]) };
        }

        this._legendSource.Update(ChartLegendSwatch.Line, entries);
    }

    /// <inheritdoc />
    [WinRT.DynamicWindowsRuntimeCast(typeof(Grid))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(Border))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(TextBlock))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(StackPanel))]
    protected override void OnApplyTemplate()
    {
        this._contextMenu?.Detach();
        this._contextMenu = null;
        this._hoverTimer?.Stop();
        this._tooltipMotion?.Reset();
        this._tooltipMotion = null;
        this.ResetTooltipRows();
        this.DestroyCanvas();

        base.OnApplyTemplate();

        this._canvasHost = this.GetTemplateChild(CanvasHostPartName) as Grid;
        this._tooltipBorder = this.GetTemplateChild(TooltipPartName) as Border;
        this._tooltipTimeText = this.GetTemplateChild(TooltipTimePartName) as TextBlock;
        this._tooltipValuesPanel = this.GetTemplateChild(TooltipValuesPartName) as StackPanel;
        if (this._canvasHost is { } host)
        {
            this._contextMenu = new ChartContextMenuPresenter(this, host, this.ReadContext, this.HideInspection);
        }

        this.UpdatePresentationState();
        this.UpdateInspectionTooltip();
        this.CreateCanvas();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        this.UpdateEffectiveGridColor();
        this.CreateCanvas();

        if (this._canvas is not null)
        {
            this._canvas.Paused = this._isPaused;
            // A graph paused before its first load still needs an initial frame.
            this._canvas.Invalidate();
        }

        if (this._isInspectionVisible)
        {
            this._inspectionTimer.Start();
            this.UpdateInspectionTooltip();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        this._hoverTimer?.Stop();
        this._isPointerOver = false;
        this._inspectionTimer.Stop();
        this._tooltipMotion?.Reset();
        this.DestroyCanvas();
    }

    private void CreateCanvas()
    {
        if (this._canvas is not null || this._canvasHost is null)
        {
            return;
        }

        var canvas = new CanvasAnimatedControl
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            ClearColor = Color.FromArgb(0, 0, 0, 0),
            TargetElapsedTime = RenderInterval,
            Paused = this._isPaused || !this.IsLoaded,
        };
        canvas.Draw += this.OnCanvasDraw;
        canvas.SizeChanged += OnCanvasSizeChanged;

        this._canvas = canvas;
        this._canvasHost.Children.Insert(0, canvas);
    }

    private void DestroyCanvas()
    {
        var canvas = this._canvas;
        if (canvas is null)
        {
            return;
        }

        this._canvas = null;
        canvas.Draw -= this.OnCanvasDraw;
        canvas.SizeChanged -= OnCanvasSizeChanged;
        canvas.Paused = true;
        this._canvasHost?.Children.Remove(canvas);
        canvas.RemoveFromVisualTree();

        this._dashedEdgeLineStyle?.Dispose();
        this._dashedEdgeLineStyle = null;
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(CanvasAnimatedControl))]
    private static void OnCanvasSizeChanged(object sender, SizeChangedEventArgs args)
    {
        // Resizing recreates the surface even when the animation loop is paused.
        ((CanvasAnimatedControl)sender).Invalidate();
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        this.UpdateEffectiveGridColor();
        this._canvas?.Invalidate();
    }

    private void UpdateEffectiveGridColor()
    {
        var isLight = this.ActualTheme == ElementTheme.Light;
        var gridColor = isLight ? Color.FromArgb(28, 0, 0, 0) : Color.FromArgb(28, 255, 255, 255);
        var inspectionLineColor = isLight ? Color.FromArgb(120, 0, 0, 0) : Color.FromArgb(136, 255, 255, 255);
        var inspectionPointOutlineColor = isLight ? Color.FromArgb(230, 255, 255, 255) : Color.FromArgb(230, 28, 28, 28);

        lock (this._syncRoot)
        {
            this._effectiveGridColor = gridColor;
            this._effectiveInspectionLineColor = inspectionLineColor;
            this._effectiveInspectionPointOutlineColor = inspectionPointOutlineColor;
        }
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new LiveAreaGraphAutomationPeer(this);
    }

    private static void OnTooltipEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var graph = (LiveAreaGraph)sender;
        if (!(bool)args.NewValue)
        {
            graph.HideInspection();
        }
    }

    private static void OnTooltipPresentationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var graph = (LiveAreaGraph)sender;
        graph.UpdateInspectionTooltip();
        graph.RefreshLegend();
    }

    private static void OnPlotPresentationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var graph = (LiveAreaGraph)sender;
        lock (graph._syncRoot)
        {
            graph._isSparkline = graph.IsSparkline;
            graph._showAreaFill = graph.ShowAreaFill;
            graph._showLatestValueMarker = graph.ShowLatestValueMarker;
        }

        if (args.Property == IsSparklineProperty)
        {
            graph.UpdatePresentationState();
        }
        else if (args.Property == ShowLatestValueMarkerProperty)
        {
            graph.UpdateInspectionTooltip();
        }

        graph._canvas?.Invalidate();
    }

    private void UpdatePresentationState()
    {
        VisualStateManager.GoToState(this, this.IsSparkline ? "Sparkline" : "Standard", false);
    }

    private static void OnInterpolationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var graph = (LiveAreaGraph)sender;
        lock (graph._syncRoot)
        {
            graph._interpolation = (LiveGraphInterpolation)args.NewValue;
            if (graph._samples.Count > 0)
            {
                var first = graph._samples[0];
                graph._samples[0] = first with
                {
                    TransitionDuration = first.TransitionDuration == TimeSpan.Zero ? TimeSpan.Zero : first.RequestedDuration,
                    SmoothControls = null,
                };
            }

            for (var index = 1; index < graph._samples.Count; index++)
            {
                var sample = graph._samples[index];
                graph._samples[index] = graph.CreateSample(
                    graph._samples[index - 1], sample.Timestamp, sample.TargetValues, sample.RequestedDuration);
            }
        }

        graph.UpdateInspectionTooltip();
        graph._canvas?.Invalidate();
    }

    private void OnPointerEnteredOrMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!this.CanInspect || this._canvas is null)
        {
            return;
        }

        var point = e.GetCurrentPoint(this._canvas).Position;
        this._isPointerOver = true;
        this.PreviewInspection(point);
    }

    private void PreviewInspection(Point point)
    {
        var delay = ChartInspection.GetHoverDelay(this);
        if (this._isInspectionVisible || delay <= TimeSpan.Zero)
        {
            this.ShowInspection(point.X, point.Y);
            return;
        }

        this._pendingInspection = point;
        if (this._hoverTimer is null)
        {
            this._hoverTimer = this.DispatcherQueue.CreateTimer();
            this._hoverTimer.IsRepeating = false;
            this._hoverTimer.Tick += this.OnHoverDelayElapsed;
        }

        if (!this._hoverTimer.IsRunning)
        {
            this._hoverTimer.Interval = delay;
            this._hoverTimer.Start();
        }
    }

    private void OnHoverDelayElapsed(DispatcherQueueTimer sender, object args)
    {
        if (this._isPointerOver && this.CanInspect)
        {
            this.ShowInspection(this._pendingInspection.X, this._pendingInspection.Y);
        }
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        this._hoverTimer?.Stop();
        this._isPointerOver = false;
        if (!this._isKeyboardInspecting)
        {
            this.HideInspection();
        }
    }

    private void OnGotFocus(object sender, RoutedEventArgs e)
    {
        if (!this.CanInspect || this._canvas is null)
        {
            return;
        }

        this._isKeyboardInspecting = true;
        if (!this._isPointerOver)
        {
            this.ShowInspection(this._canvas.ActualWidth, this._canvas.ActualHeight / 2);
        }
    }

    private void OnLostFocus(object sender, RoutedEventArgs e)
    {
        this._isKeyboardInspecting = false;
        if (!this._isPointerOver)
        {
            this.HideInspection();
        }
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Handled || !this.CanInspect || this._canvas is null)
        {
            return;
        }

        var width = this._canvas.ActualWidth;
        var height = this._canvas.ActualHeight;
        var inset = GetPlotInset(width, height, this.ShowLatestValueMarker);
        double seconds;
        lock (this._syncRoot)
        {
            seconds = this._historyDuration.TotalSeconds;
        }

        var step = Math.Max(4, (width - 2 * inset) / Math.Max(1, seconds));
        var handled = true;

        switch (e.Key)
        {
            case VirtualKey.Left:
                this._inspectionX -= step;
                break;
            case VirtualKey.Right:
                this._inspectionX += step;
                break;
            case VirtualKey.Home:
                this._inspectionX = inset;
                break;
            case VirtualKey.End:
                this._inspectionX = width - inset;
                break;
            case VirtualKey.Escape:
                this._isKeyboardInspecting = false;
                this.HideInspection();
                e.Handled = true;
                return;
            default:
                handled = false;
                break;
        }

        if (!handled)
        {
            return;
        }

        this._isKeyboardInspecting = true;
        this.ShowInspection(Math.Clamp(this._inspectionX, inset, width - inset), Math.Clamp(this._inspectionY, 0, height));
        e.Handled = true;
    }

    private void ShowInspection(double x, double y)
    {
        this._hoverTimer?.Stop();
        if (this._canvas is null)
        {
            return;
        }

        lock (this._syncRoot)
        {
            this._inspectionX = Math.Clamp(x, 0, this._canvas.ActualWidth);
            this._inspectionY = Math.Clamp(y, 0, this._canvas.ActualHeight);
            this._isInspectionVisible = true;
        }

        this._inspectionTimer.Start();
        this.UpdateInspectionTooltip();
        this._canvas.Invalidate();
    }

    private void HideInspection()
    {
        this._hoverTimer?.Stop();
        lock (this._syncRoot)
        {
            this._isInspectionVisible = false;
        }

        this._inspectionTimer.Stop();
        this.HideInspectionTooltip();
        this._canvas?.Invalidate();
    }

    private void HideInspectionTooltip()
    {
        this._tooltipMotion?.Reset();
        if (this._customTooltipPresenter is { } presenter)
        {
            presenter.Content = null;
            this._customTooltipContent = null;
        }

        if (this._tooltipBorder is not null)
        {
            this._tooltipBorder.Visibility = Visibility.Collapsed;
        }
    }

    private void OnInspectionTimerTick(object? sender, object e)
    {
        this.UpdateInspectionTooltip();
        this._canvas?.Invalidate();
    }

    private void UpdateInspectionTooltip()
    {
        if (this._tooltipBorder is not { } tooltip
            || this._tooltipTimeText is null
            || this._tooltipValuesPanel is null
            || this._canvas is null)
        {
            return;
        }

        var width = this._canvas.ActualWidth;
        var height = this._canvas.ActualHeight;
        var snapshot = this._isInspectionVisible ? this.ReadInspectionSnapshot(this._inspectionX, this._inspectionY, width, height) : null;

        if (snapshot is null)
        {
            this.HideInspectionTooltip();
            return;
        }

        if (this._tooltipSeries != snapshot.Series)
        {
            this.RebuildTooltipRows(snapshot.Series);
        }

        if (this._customTooltipPresenter is { } presenter)
        {
            this._customTooltipContent ??= new ChartInspectionContent();
            var values = ReadInspectionValues(snapshot);
            var age = FormatAge(snapshot.Age);
            var rows = new ChartInspectionRow[values.Length + 1];
            rows[0] = new ChartInspectionRow(age);
            var text = new StringBuilder(age);
            for (var index = 0; index < values.Length; index++)
            {
                var line = WinUIResourceStrings.FormatDefault(
                    "LabelValueFormat", snapshot.Series[index].Name, this.FormatTooltipValue(snapshot.Values[index]));
                rows[index + 1] = new ChartInspectionRow(line, this._tooltipSwatches[index]);
                text.AppendLine().Append(line);
            }

            this._customTooltipContent.Update(text.ToString(), rows, values);
            if (presenter.Content != this._customTooltipContent)
            {
                presenter.Content = this._customTooltipContent;
            }
        }
        else
        {
            this._tooltipTimeText.Text = FormatAge(snapshot.Age);
            for (var index = 0; index < snapshot.Values.Length && index < this._tooltipValueTexts.Count; index++)
            {
                this._tooltipValueTexts[index].Text = this.FormatTooltipValue(snapshot.Values[index]);
            }
        }

        tooltip.Visibility = Visibility.Visible;
        tooltip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var tooltipSize = tooltip.DesiredSize;
        const double margin = 8;
        const double gap = 12;
        var opensRight = snapshot.X + gap + tooltipSize.Width <= width;
        var left = opensRight ? snapshot.X + gap : snapshot.X - tooltipSize.Width - gap;
        var top = snapshot.Y - tooltipSize.Height / 2;

        this._tooltipMotion ??= new ChartInspectorMotion(
            this, position =>
            {
                Canvas.SetLeft(tooltip, position.X);
                Canvas.SetTop(tooltip, position.Y);
            });
        var limits = new Rect(
            margin, margin, Math.Max(0, width - tooltipSize.Width - 2 * margin), Math.Max(0, height - tooltipSize.Height - 2 * margin));
        this._tooltipMotion.MoveTo(new Point(left, top), limits, opensRight);
    }

    private ChartContextTarget? ReadContext(Point? position)
    {
        if (this._canvas is not { } canvas
            || this._canvasHost is not { } host
            || (position is null && !(this._isKeyboardInspecting && this._isInspectionVisible)))
        {
            return null;
        }

        var point = position is Point pointer
            ? host.TransformToVisual(canvas).TransformPoint(pointer)
            : new Point(this._inspectionX, this._inspectionY);
        if (this.ReadInspectionSnapshot(point.X, point.Y, canvas.ActualWidth, canvas.ActualHeight) is not { } snapshot)
        {
            return null;
        }

        var values = ReadInspectionValues(snapshot);
        var text = new StringBuilder(FormatAge(snapshot.Age));
        for (var index = 0; index < values.Length; index++)
        {
            text.AppendLine().Append(
                WinUIResourceStrings.FormatDefault(
                    "LabelValueFormat", snapshot.Series[index].Name, this.FormatTooltipValue(snapshot.Values[index])));
        }

        var anchor = canvas.TransformToVisual(host).TransformPoint(new Point(snapshot.X, snapshot.Y));
        return new ChartContextTarget(new ChartInspectionItem(anchor, text.ToString()), values);
    }

    private static ChartContextValue[] ReadInspectionValues(InspectionSnapshot snapshot)
    {
        var values = new ChartContextValue[snapshot.Values.Length];
        for (var index = 0; index < values.Length; index++)
        {
            var series = snapshot.Series[index];
            values[index] = new ChartContextValue(series.Name, snapshot.Values[index], series, index, Timestamp: snapshot.Timestamp);
        }

        return values;
    }

    private InspectionSnapshot? ReadInspectionSnapshot(double x, double y, double width, double height)
    {
        lock (this._syncRoot)
        {
            if (this._samples.Count == 0 || this._seriesSnapshot.Length == 0 || width <= 0 || height <= 0)
            {
                return null;
            }

            var graphTime = this._isPaused ? this._pausedAt : this._clock.Now;
            x = Math.Clamp(x, 0, width);
            var inset = GetPlotInset(width, height, this._showLatestValueMarker);
            var timestamp = GetInspectionTimestampCore(x - inset, width - 2 * inset, graphTime, this._historyDuration);
            return this.TryGetValuesAtTimeCore(timestamp, out var values)
                ? new InspectionSnapshot(graphTime - timestamp, x, Math.Clamp(y, 0, height), this._seriesSnapshot, values, timestamp)
                : null;
        }
    }

    private static string FormatAge(TimeSpan age)
    {
        return age.TotalMilliseconds < 50
            ? WinUIResourceStrings.Get("LiveGraphNow")
            : WinUIResourceStrings.FormatDefault("LiveGraphSecondsAgoFormat", age.TotalSeconds);
    }

    private void ResetTooltipRows()
    {
        this._tooltipSeries = null;
        this._tooltipValuesPanel?.Children.Clear();
        this._tooltipValueTexts.Clear();
        this._customTooltipPresenter = null;
        this._customTooltipContent = null;
        this._tooltipSwatches = [];
    }

    private void RebuildTooltipRows(LiveGraphSeries[] series)
    {
        if (this._tooltipValuesPanel is null)
        {
            return;
        }

        this.ResetTooltipRows();
        this._tooltipSeries = series;
        var template = ChartInspection.GetContentTemplate(this);
        if (this._tooltipTimeText is not null)
        {
            this._tooltipTimeText.Visibility = template is null ? Visibility.Visible : Visibility.Collapsed;
        }

        if (template is not null)
        {
            this._tooltipSwatches = series.Select(graphSeries => (Brush)new SolidColorBrush(graphSeries.EdgeLineColor)).ToArray();
            this._customTooltipPresenter = new ContentPresenter { ContentTemplate = template };
            this._tooltipValuesPanel.Children.Add(this._customTooltipPresenter);
            return;
        }

        foreach (var graphSeries in series)
        {
            var row = new Grid { ColumnSpacing = 8, MinWidth = 166, };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var swatch = new Ellipse
            {
                Width = 8,
                Height = 8,
                VerticalAlignment = VerticalAlignment.Center,
                Fill = new SolidColorBrush(graphSeries.EdgeLineColor),
            };
            var nameText = new TextBlock
            {
                MaxWidth = 96,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
                Opacity = 0.78,
                Text = graphSeries.Name,
                TextTrimming = TextTrimming.CharacterEllipsis,
            };
            var valueText = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                TextAlignment = TextAlignment.Right,
            };

            Grid.SetColumn(nameText, 1);
            Grid.SetColumn(valueText, 2);
            row.Children.Add(swatch);
            row.Children.Add(nameText);
            row.Children.Add(valueText);
            this._tooltipValuesPanel.Children.Add(row);
            this._tooltipValueTexts.Add(valueText);
        }
    }

    private string FormatTooltipValue(double value)
    {
        string formatted;
        try
        {
            formatted = value.ToString(this.TooltipValueFormat, CultureInfo.CurrentCulture);
        }
        catch (FormatException)
        {
            formatted = value.ToString("G", CultureInfo.CurrentCulture);
        }

        return formatted + this.TooltipValueSuffix;
    }

    private void TrimSamplesCore(DateTimeOffset newestTimestamp)
    {
        var visibleStart = GetWindowStart(newestTimestamp, this._historyDuration);
        var retentionCutoff = GetWindowStart(visibleStart, TimeSpan.FromSeconds(2));
        var removeCount = 0;

        while (removeCount < this._samples.Count - 1 && this._samples[removeCount + 1].Timestamp < retentionCutoff)
        {
            removeCount++;
        }

        if (removeCount > 0)
        {
            this._samples.RemoveRange(0, removeCount);
        }
    }

    private void OnCanvasDraw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
    {
        var width = (float)sender.Size.Width;
        var height = (float)sender.Size.Height;

        if (width <= 0 || height <= 0)
        {
            return;
        }

        var snapshot = this.CaptureRenderSnapshot(out var rentedSamples);
        var drawingSession = args.DrawingSession;
        var inset = (float)GetPlotInset(width, height, snapshot.ShowLatestValueMarker);
        var originalTransform = inset > 0 ? drawingSession.Transform : Matrix3x2.Identity;
        try
        {
            if (inset > 0)
            {
                drawingSession.Transform = Matrix3x2.CreateTranslation(inset, inset) * originalTransform;
                width -= 2 * inset;
                height -= 2 * inset;
            }

            this.DrawGrid(drawingSession, width, height, snapshot);
            this.DrawSeries(drawingSession, sender.Device, width, height, inset, snapshot);
            this.DrawInspection(drawingSession, width, height, inset, snapshot);
        }
        finally
        {
            if (rentedSamples is not null)
            {
                ArrayPool<GraphSample>.Shared.Return(rentedSamples, clearArray: true);
            }

            if (inset > 0)
            {
                drawingSession.Transform = originalTransform;
            }
        }
    }

    private static double GetPlotInset(double width, double height, bool showLatestValueMarker)
    {
        return showLatestValueMarker ? Math.Min(PointOutlineRadius + 0.75, Math.Min(width, height) / 4) : 0;
    }

    private RenderSnapshot CaptureRenderSnapshot(out GraphSample[]? rentedSamples)
    {
        rentedSamples = null;
        var now = this._clock.Now;

        lock (this._syncRoot)
        {
            var graphTime = this._isPaused ? this._pausedAt : now;
            var sampleCount = 0;
            GraphSample[] samples = [];

            if (this._samples.Count > 0)
            {
                var visibleStart = GetWindowStart(graphTime, this._historyDuration);
                var firstSampleIndex = 0;
                while (firstSampleIndex < this._samples.Count - 1 && this._samples[firstSampleIndex + 1].Timestamp < visibleStart)
                {
                    firstSampleIndex++;
                }

                var lastSampleIndex = this._samples.Count - 1;
                while (lastSampleIndex >= firstSampleIndex && this._samples[lastSampleIndex].Timestamp > graphTime)
                {
                    lastSampleIndex--;
                }

                if (lastSampleIndex >= firstSampleIndex)
                {
                    sampleCount = lastSampleIndex - firstSampleIndex + 1;
                    rentedSamples = ArrayPool<GraphSample>.Shared.Rent(sampleCount);
                    this._samples.CopyTo(firstSampleIndex, rentedSamples, 0, sampleCount);
                    samples = rentedSamples;
                }
            }

            return new RenderSnapshot(
                graphTime, this._historyDuration, this._isSparkline ? LiveGraphGridMode.None : this._gridMode, this._gridSpacing,
                this._minimum, this._maximum, this._effectiveGridColor, this._effectiveInspectionLineColor,
                this._effectiveInspectionPointOutlineColor, this._showAreaFill, this._showLatestValueMarker, this._isInspectionVisible,
                this._inspectionX, this._seriesSnapshot, samples, sampleCount);
        }
    }

    private void DrawGrid(CanvasDrawingSession drawingSession, float width, float height, RenderSnapshot snapshot)
    {
        if (snapshot.GridMode == LiveGraphGridMode.None)
        {
            return;
        }

        var spacing = (float)snapshot.GridSpacing;
        var pixelsPerSecond = width / (float)snapshot.HistoryDuration.TotalSeconds;
        var elapsedSeconds = snapshot.GraphTime.ToUnixTimeMilliseconds() / 1000d;
        var phase = (float)((elapsedSeconds * pixelsPerSecond) % spacing);
        var rightmostX = width - phase;

        if (snapshot.GridMode == LiveGraphGridMode.Cross)
        {
            for (var x = rightmostX; x >= 0; x -= spacing)
            {
                drawingSession.DrawLine(x, 0, x, height, snapshot.GridColor, 1);
            }

            for (var x = rightmostX + spacing; x <= width; x += spacing)
            {
                drawingSession.DrawLine(x, 0, x, height, snapshot.GridColor, 1);
            }

            for (var y = height; y >= 0; y -= spacing)
            {
                drawingSession.DrawLine(0, y, width, y, snapshot.GridColor, 1);
            }

            return;
        }

        for (var y = height; y >= 0; y -= spacing)
        {
            for (var x = rightmostX; x >= 0; x -= spacing)
            {
                drawingSession.FillCircle(x, y, 1.15f, snapshot.GridColor);
            }

            for (var x = rightmostX + spacing; x <= width; x += spacing)
            {
                drawingSession.FillCircle(x, y, 1.15f, snapshot.GridColor);
            }
        }
    }

    private void DrawSeries(
        CanvasDrawingSession drawingSession,
        CanvasDevice device,
        float width,
        float height,
        float inset,
        RenderSnapshot snapshot)
    {
        if (snapshot.Series.Length == 0 || snapshot.SampleCount == 0)
        {
            return;
        }

        var geometries = new List<SeriesGeometry>(snapshot.Series.Length);

        try
        {
            for (var seriesIndex = 0; seriesIndex < snapshot.Series.Length; seriesIndex++)
            {
                geometries.Add(this.CreateSeriesGeometry(device, seriesIndex, width, height, snapshot));
            }

            for (var index = 0; index < geometries.Count; index++)
            {
                if (geometries[index].Fill is { } fill)
                {
                    drawingSession.FillGeometry(fill, snapshot.Series[index].FillColor);
                }
            }

            for (var index = 0; index < geometries.Count; index++)
            {
                var series = snapshot.Series[index];
                if (series.EdgeLineStyle == LiveGraphEdgeLineStyle.None)
                {
                    continue;
                }

                if (series.EdgeLineStyle == LiveGraphEdgeLineStyle.Dashed)
                {
                    this._dashedEdgeLineStyle ??= new CanvasStrokeStyle { DashStyle = CanvasDashStyle.Dash, };

                    drawingSession.DrawGeometry(
                        geometries[index].Stroke, series.EdgeLineColor, series.EdgeLineThickness, this._dashedEdgeLineStyle);
                }
                else
                {
                    drawingSession.DrawGeometry(geometries[index].Stroke, series.EdgeLineColor, series.EdgeLineThickness);
                }
            }

            if (snapshot.ShowLatestValueMarker)
            {
                var radius = Math.Min(PointOutlineRadius, inset);
                for (var index = 0; index < geometries.Count; index++)
                {
                    DrawPointMarker(
                        drawingSession, geometries[index].EndPoint, radius, snapshot.InspectionPointOutlineColor,
                        snapshot.Series[index].EdgeLineColor);
                }
            }
        }
        finally
        {
            foreach (var geometry in geometries)
            {
                geometry.Dispose();
            }
        }
    }

    private void DrawInspection(CanvasDrawingSession drawingSession, float width, float height, float inset, RenderSnapshot snapshot)
    {
        if (!snapshot.IsInspectionVisible)
        {
            return;
        }

        var x = (float)Math.Clamp(snapshot.InspectionX - inset, 0, width);
        drawingSession.DrawLine(x, 0, x, height, snapshot.InspectionLineColor, 1.25f);

        var timestamp = GetInspectionTimestampCore(x, width, snapshot.GraphTime, snapshot.HistoryDuration);
        if (!TryGetValuesAtTimeCore(snapshot.Samples, snapshot.SampleCount, snapshot.Series.Length, timestamp, out var values))
        {
            return;
        }

        for (var index = 0; index < values.Length && index < snapshot.Series.Length; index++)
        {
            var normalized = ChartScale.Normalize(values[index], snapshot.Minimum, snapshot.Maximum);
            var y = height - ((float)normalized * height);

            DrawPointMarker(
                drawingSession, new Vector2(x, y), PointOutlineRadius, snapshot.InspectionPointOutlineColor,
                snapshot.Series[index].EdgeLineColor);
        }
    }

    private static void DrawPointMarker(CanvasDrawingSession drawingSession, Vector2 center, float radius, Color outline, Color fill)
    {
        drawingSession.FillCircle(center.X, center.Y, radius, outline);
        drawingSession.FillCircle(center.X, center.Y, radius * (PointFillRadius / PointOutlineRadius), fill);
    }

    private SeriesGeometry CreateSeriesGeometry(CanvasDevice device, int seriesIndex, float width, float height, RenderSnapshot snapshot)
    {
        using var fillBuilder = snapshot.ShowAreaFill ? new CanvasPathBuilder(device) : null;
        using var strokeBuilder = new CanvasPathBuilder(device);

        var points = this.CreateSeriesPoints(seriesIndex, width, height, snapshot);

        var firstPoint = points[0];
        fillBuilder?.BeginFigure(new Vector2(firstPoint.X, height));
        fillBuilder?.AddLine(firstPoint);
        strokeBuilder.BeginFigure(firstPoint);

        var lastPoint = firstPoint;
        for (var pointIndex = 1; pointIndex < points.Count; pointIndex++)
        {
            lastPoint = points[pointIndex];
            fillBuilder?.AddLine(lastPoint);
            strokeBuilder.AddLine(lastPoint);
        }

        fillBuilder?.AddLine(new Vector2(lastPoint.X, height));
        fillBuilder?.EndFigure(CanvasFigureLoop.Closed);
        strokeBuilder.EndFigure(CanvasFigureLoop.Open);

        return new SeriesGeometry(
            fillBuilder is null ? null : CanvasGeometry.CreatePath(fillBuilder), CanvasGeometry.CreatePath(strokeBuilder), lastPoint);
    }

    private List<Vector2> CreateSeriesPoints(int seriesIndex, float width, float height, RenderSnapshot snapshot)
    {
        var points = new List<Vector2>();
        var visibleStart = GetWindowStart(snapshot.GraphTime, snapshot.HistoryDuration);

        void AddPoint(DateTimeOffset timestamp, double value)
        {
            var point = this.GetPoint(timestamp, value, visibleStart, width, height, snapshot);
            if (points.Count == 0 || points[^1] != point)
            {
                points.Add(point);
            }
        }

        var firstSample = snapshot.Samples[0];
        if (firstSample.Timestamp > visibleStart)
        {
            AddPoint(visibleStart, firstSample.StartValues[seriesIndex]);
        }

        for (var sampleIndex = 0; sampleIndex < snapshot.SampleCount; sampleIndex++)
        {
            var sample = snapshot.Samples[sampleIndex];
            var segmentStart = sample.Timestamp < visibleStart ? visibleStart : sample.Timestamp;
            var segmentEnd = sampleIndex < snapshot.SampleCount - 1 ? snapshot.Samples[sampleIndex + 1].Timestamp : snapshot.GraphTime;

            if (segmentEnd > snapshot.GraphTime)
            {
                segmentEnd = snapshot.GraphTime;
            }

            if (segmentEnd < visibleStart || segmentStart > snapshot.GraphTime || segmentEnd < segmentStart)
            {
                continue;
            }

            AddPoint(segmentStart, GetRenderedValue(sample, seriesIndex, segmentStart));

            if (sample.TransitionDuration > TimeSpan.Zero)
            {
                var visibleDuration = segmentEnd - sample.Timestamp;
                var visibleTransitionEnd = visibleDuration < sample.TransitionDuration
                    ? segmentEnd
                    : sample.Timestamp + sample.TransitionDuration;

                if (visibleTransitionEnd > segmentStart)
                {
                    var segmentCount = (int)Math.Clamp(Math.Ceiling(sample.TransitionDuration.TotalMilliseconds / 50d), 2, 32);

                    for (var transitionIndex = 1; transitionIndex < segmentCount; transitionIndex++)
                    {
                        var offset = sample.TransitionDuration * (transitionIndex / (double)segmentCount);

                        if (offset > segmentStart - sample.Timestamp && offset < visibleTransitionEnd - sample.Timestamp)
                        {
                            var transitionTime = sample.Timestamp + offset;
                            AddPoint(transitionTime, GetRenderedValue(sample, seriesIndex, transitionTime));
                        }
                    }

                    AddPoint(visibleTransitionEnd, GetRenderedValue(sample, seriesIndex, visibleTransitionEnd));
                }
            }

            // Hold the most recent sampled value through the unsampled interval. For the
            // newest sample this point is always at the right edge, so even a five-second
            // cadence scrolls continuously instead of revealing a wide segment at once.
            AddPoint(segmentEnd, GetRenderedValue(sample, seriesIndex, segmentEnd));
        }

        return points;
    }

    private Vector2 GetPoint(
        DateTimeOffset timestamp,
        double value,
        DateTimeOffset visibleStart,
        float width,
        float height,
        RenderSnapshot snapshot)
    {
        var timeRange = (snapshot.GraphTime - visibleStart).TotalMilliseconds;
        var sampleOffset = (timestamp - visibleStart).TotalMilliseconds;
        var x = timeRange > 0 ? (float)(sampleOffset / timeRange * width) : width;

        var normalized = ChartScale.Normalize(value, snapshot.Minimum, snapshot.Maximum);
        var y = height - ((float)normalized * height);

        return new Vector2(x, y);
    }

    private bool TryGetValuesAtTimeCore(DateTimeOffset timestamp, out double[] values)
    {
        return TryGetValuesAtTimeCore(this._samples, this._samples.Count, this._seriesSnapshot.Length, timestamp, out values);
    }

    private static DateTimeOffset GetWindowStart(DateTimeOffset end, TimeSpan duration)
    {
        return new DateTimeOffset(Math.Max(0, end.UtcTicks - duration.Ticks), TimeSpan.Zero);
    }

    private static DateTimeOffset GetInspectionTimestampCore(double x, double width, DateTimeOffset graphTime, TimeSpan historyDuration)
    {
        var position = width <= 0 ? 1 : Math.Clamp(x / width, 0, 1);
        var start = GetWindowStart(graphTime, historyDuration);
        var ticks = (graphTime - start).Ticks;
        return start.AddTicks(Math.Min(ticks, (long)(ticks * position)));
    }

    private static bool TryGetValuesAtTimeCore(
        IReadOnlyList<GraphSample> samples,
        int sampleCount,
        int seriesCount,
        DateTimeOffset timestamp,
        out double[] values)
    {
        if (sampleCount == 0 || seriesCount == 0)
        {
            values = [];
            return false;
        }

        var low = 0;
        var high = sampleCount - 1;
        var sampleIndex = -1;
        while (low <= high)
        {
            var middle = low + ((high - low) / 2);
            if (samples[middle].Timestamp <= timestamp)
            {
                sampleIndex = middle;
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        var sample = sampleIndex >= 0 ? samples[sampleIndex] : samples[0];
        values = new double[seriesCount];
        for (var index = 0; index < values.Length; index++)
        {
            values[index] = GetRenderedValue(sample, index, timestamp);
        }

        return true;
    }

    private static double GetRenderedValue(GraphSample sample, int seriesIndex, DateTimeOffset graphTime)
    {
        if (sample.TransitionDuration == TimeSpan.Zero)
        {
            return sample.TargetValues[seriesIndex];
        }

        var elapsed = graphTime - sample.Timestamp;
        if (elapsed <= TimeSpan.Zero)
        {
            return sample.StartValues[seriesIndex];
        }

        if (elapsed >= sample.TransitionDuration)
        {
            return sample.TargetValues[seriesIndex];
        }

        var progress = elapsed / sample.TransitionDuration;
        var start = sample.StartValues[seriesIndex];
        var target = sample.TargetValues[seriesIndex];
        var easedProgress = progress * progress * (3 - 2 * progress);
        if (sample.SmoothControls is { } controls)
        {
            var firstWeight = 3 * progress / (1 + 2 * progress);
            start = Blend(start, controls[seriesIndex], firstWeight);
        }

        return Blend(start, target, easedProgress);
    }

    private GraphSample CreateSample(GraphSample? previous, DateTimeOffset timestamp, double[] targets, TimeSpan requestedDuration)
    {
        if (previous is null || requestedDuration == TimeSpan.Zero)
        {
            return new GraphSample(timestamp, targets, targets, requestedDuration, TimeSpan.Zero, null);
        }

        var elapsed = timestamp - previous.Timestamp;
        var smooth = this._interpolation == LiveGraphInterpolation.Smooth;
        var duration = requestedDuration;
        if (smooth && requestedDuration.Ticks <= long.MaxValue / 2)
        {
            duration *= Math.Clamp(1.5 * (elapsed / requestedDuration), 1, 2);
        }

        var starts = new double[targets.Length];
        var controls = smooth ? new double[targets.Length] : null;
        for (var index = 0; index < starts.Length; index++)
        {
            var start = GetRenderedValue(previous, index, timestamp);
            starts[index] = start;
            if (controls is not null)
            {
                controls[index] = GetFirstControl(previous, index, elapsed, duration, start, targets[index]);
            }
        }

        return new GraphSample(timestamp, starts, targets, requestedDuration, duration, controls);
    }

    private static double GetFirstControl(GraphSample previous, int index, TimeSpan elapsed, TimeSpan duration, double start, double target)
    {
        if (previous.TransitionDuration == TimeSpan.Zero || elapsed >= previous.TransitionDuration || start == target)
        {
            return start;
        }

        var first = previous.StartValues[index];
        var control = previous.SmoothControls?[index] ?? first;
        var last = previous.TargetValues[index];
        var scale = Math.Max(Math.Max(Math.Abs(first), Math.Abs(last)), Math.Abs(target));
        var progress = elapsed / previous.TransitionDuration;
        var remaining = 1 - progress;
        // One third of the previous cubic's slope, scaled to the new duration.
        var rate = ((control / scale - first / scale) * remaining + 2 * (last / scale - control / scale) * progress) * remaining;
        var next = start / scale + rate * (duration / previous.TransitionDuration);
        return Math.Clamp(next * scale, Math.Min(start, target), Math.Max(start, target));
    }

    private static double Blend(double start, double target, double amount)
    {
        return Math.Clamp((1 - amount) * start + amount * target, Math.Min(start, target), Math.Max(start, target));
    }

    private readonly record struct RenderSnapshot(
        DateTimeOffset GraphTime,
        TimeSpan HistoryDuration,
        LiveGraphGridMode GridMode,
        double GridSpacing,
        double Minimum,
        double Maximum,
        Color GridColor,
        Color InspectionLineColor,
        Color InspectionPointOutlineColor,
        bool ShowAreaFill,
        bool ShowLatestValueMarker,
        bool IsInspectionVisible,
        double InspectionX,
        LiveGraphSeries[] Series,
        GraphSample[] Samples,
        int SampleCount);

    private sealed record GraphSample(
        DateTimeOffset Timestamp,
        double[] StartValues,
        double[] TargetValues,
        TimeSpan RequestedDuration,
        TimeSpan TransitionDuration,
        double[]? SmoothControls);

    private sealed record InspectionSnapshot(
        TimeSpan Age,
        double X,
        double Y,
        LiveGraphSeries[] Series,
        double[] Values,
        DateTimeOffset Timestamp);

    private sealed partial class LiveAreaGraphAutomationPeer(LiveAreaGraph owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore()
        {
            return nameof(LiveAreaGraph);
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Group;
        }
    }

    private sealed partial class SeriesGeometry(CanvasGeometry? fill, CanvasGeometry stroke, Vector2 endPoint) : IDisposable
    {
        public CanvasGeometry? Fill { get; } = fill;

        public CanvasGeometry Stroke { get; } = stroke;

        public Vector2 EndPoint { get; } = endPoint;

        public void Dispose()
        {
            this.Fill?.Dispose();
            this.Stroke.Dispose();
        }
    }
}
