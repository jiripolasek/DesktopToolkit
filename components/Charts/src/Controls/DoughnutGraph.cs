// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Globalization;
using System.Numerics;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>
/// Displays animated proportional slices as a doughnut or a filled pie.
/// </summary>
[TemplatePart(Name = CanvasHostPartName, Type = typeof(Grid))]
[TemplatePart(Name = LegendPartName, Type = typeof(ScrollViewer))]
[TemplatePart(Name = ChartInspectorPresenter.PartName, Type = typeof(Popup))]
[TemplateVisualState(Name = "Doughnut", GroupName = "ShapeStates")]
[TemplateVisualState(Name = "Pie", GroupName = "ShapeStates")]
[TemplateVisualState(Name = "LegendBelow", GroupName = "LegendStates")]
[TemplateVisualState(Name = "LegendBeside", GroupName = "LegendStates")]
public sealed partial class DoughnutGraph : Control
{
    private const string CanvasHostPartName = "PART_CanvasHost";
    private const string LegendPartName = "PART_Legend";
    private static readonly TimeSpan RenderInterval = TimeSpan.FromMilliseconds(1000d / 30d);

    /// <summary>Identifies the <see cref="CenterValue"/> dependency property.</summary>
    public static readonly DependencyProperty CenterValueProperty = DependencyProperty.Register(
        nameof(CenterValue), typeof(string), typeof(DoughnutGraph), new PropertyMetadata(string.Empty));

    /// <summary>Identifies the <see cref="CenterLabel"/> dependency property.</summary>
    public static readonly DependencyProperty CenterLabelProperty = DependencyProperty.Register(
        nameof(CenterLabel), typeof(string), typeof(DoughnutGraph), new PropertyMetadata(string.Empty));

    /// <summary>Identifies the <see cref="Mode"/> dependency property.</summary>
    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
        nameof(Mode), typeof(DoughnutGraphMode), typeof(DoughnutGraph), new PropertyMetadata(DoughnutGraphMode.Doughnut, OnModeChanged));

    /// <summary>Identifies the <see cref="ShowLegend"/> dependency property.</summary>
    public static readonly DependencyProperty ShowLegendProperty = DependencyProperty.Register(
        nameof(ShowLegend), typeof(bool), typeof(DoughnutGraph), new PropertyMetadata(false, OnShowLegendChanged));

    /// <summary>Identifies the <see cref="ValueFormat"/> dependency property.</summary>
    public static readonly DependencyProperty ValueFormatProperty = DependencyProperty.Register(
        nameof(ValueFormat), typeof(string), typeof(DoughnutGraph), new PropertyMetadata(
            null, static (sender, _) =>
            {
                var graph = (DoughnutGraph)sender;
                graph._inspector?.Refresh();
                graph.RefreshLegend();
            }));

    /// <summary>Identifies the <see cref="Data"/> dependency property.</summary>
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(DoughnutGraphData), typeof(DoughnutGraph), new PropertyMetadata(null, OnDataChanged));

    /// <summary>Identifies the <see cref="RingThickness"/> dependency property.</summary>
    public static readonly DependencyProperty RingThicknessProperty = DependencyProperty.Register(
        nameof(RingThickness), typeof(double), typeof(DoughnutGraph),
        new PropertyMetadata(14d, static (sender, args) => ((DoughnutGraph)sender).SetRingThicknessCore((double)args.NewValue)));

    /// <summary>Identifies the <see cref="GapAngle"/> dependency property.</summary>
    public static readonly DependencyProperty GapAngleProperty = DependencyProperty.Register(
        nameof(GapAngle), typeof(double), typeof(DoughnutGraph),
        new PropertyMetadata(3d, static (sender, args) => ((DoughnutGraph)sender).SetGapAngleCore((double)args.NewValue)));

    /// <summary>Identifies the <see cref="TransitionDuration"/> dependency property.</summary>
    public static readonly DependencyProperty TransitionDurationProperty = DependencyProperty.Register(
        nameof(TransitionDuration), typeof(TimeSpan), typeof(DoughnutGraph),
        new PropertyMetadata(
            TimeSpan.FromMilliseconds(420),
            static (sender, args) => ((DoughnutGraph)sender).SetTransitionDurationCore((TimeSpan)args.NewValue)));

    private readonly object _syncRoot = new();
    private readonly ChartValueDispatcher _valueDispatcher;
    private readonly List<DoughnutGraphSeries> _series = [];
    private readonly DispatcherTimer _animationTimer;
    private readonly ChartMotionPreference _motion;
    private readonly MonotonicClock _clock;
    private Grid? _canvasHost;
    private CanvasControl? _canvas;
    private DoughnutSliceRenderer? _renderer;
    private DoughnutLegendPresenter? _legend;
    private ChartInspectorPresenter? _inspector;
    private DoughnutGraphSeries[] _seriesSnapshot = [];
    private double[] _renderedValues = [];
    private double[] _rawValues = [];
    private double[] _displayedValues = [];
    private double[] _animationStartValues = [];
    private double[] _animationTargetValues = [];
    private DateTimeOffset _animationStartedAt;
    private TimeSpan _animationDuration;
    private TimeSpan _transitionDuration = TimeSpan.FromMilliseconds(420);
    private double _ringThickness = 14;
    private double _gapAngle = 3;
    private bool _hasValues;
    private bool _isAnimating;
    private bool _hasBoundData;
    private Color _effectiveTrackColor;

    /// <summary>Gets or sets the snapshot of series and values. Null returns to imperative mode.</summary>
    public DoughnutGraphData? Data
    {
        get => (DoughnutGraphData?)this.GetValue(DataProperty);
        set => this.SetValue(DataProperty, value);
    }

    /// <summary>
    /// Gets a snapshot of the configured slices, in the order expected by
    /// <see cref="SetValues(double[])"/>.
    /// </summary>
    public IReadOnlyList<DoughnutGraphSeries> Series
    {
        get
        {
            lock (this._syncRoot)
            {
                return this._series.ToArray();
            }
        }
    }

    /// <summary>Gets or sets the main text in the doughnut center, independently of slice values.</summary>
    public string CenterValue
    {
        get => (string)this.GetValue(CenterValueProperty);
        set => this.SetValue(CenterValueProperty, value);
    }

    /// <summary>Gets or sets the secondary label below the doughnut center value.</summary>
    public string CenterLabel
    {
        get => (string)this.GetValue(CenterLabelProperty);
        set => this.SetValue(CenterLabelProperty, value);
    }

    /// <summary>Gets or sets the chart shape. The default is a doughnut.</summary>
    public DoughnutGraphMode Mode
    {
        get => (DoughnutGraphMode)this.GetValue(ModeProperty);
        set => this.SetValue(ModeProperty, value);
    }

    /// <summary>Gets or sets whether to show slice names and shares in a responsive legend.</summary>
    public bool ShowLegend
    {
        get => (bool)this.GetValue(ShowLegendProperty);
        set => this.SetValue(ShowLegendProperty, value);
    }

    /// <summary>Gets or sets the format used for the slice value in the inspector.</summary>
    public string? ValueFormat
    {
        get => (string?)this.GetValue(ValueFormatProperty);
        set => this.SetValue(ValueFormatProperty, value);
    }

    /// <summary>
    /// Gets or sets the ring width in effective pixels.
    /// </summary>
    public double RingThickness
    {
        get
        {
            lock (this._syncRoot)
            {
                return this._ringThickness;
            }
        }

        set => this.SetValue(RingThicknessProperty, value);
    }

    /// <summary>
    /// Gets or sets the visual gap between non-empty doughnut slices, in degrees.
    /// </summary>
    public double GapAngle
    {
        get
        {
            lock (this._syncRoot)
            {
                return this._gapAngle;
            }
        }

        set => this.SetValue(GapAngleProperty, value);
    }

    /// <summary>
    /// Gets or sets how long changed slice values take to settle. Use zero for immediate updates.
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

    /// <summary>Initializes a new instance with the default theme and animation settings.</summary>
    public DoughnutGraph()
        : this(TimeProvider.System)
    {
    }

    internal DoughnutGraph(TimeProvider timeProvider)
    {
        this._clock = new MonotonicClock(timeProvider);
        this._valueDispatcher = new ChartValueDispatcher(this.DispatcherQueue, this.SetValuesCore);
        this._motion = new ChartMotionPreference(this, this.FinishAnimation);
        this.DefaultStyleKey = typeof(DoughnutGraph);
        ChartLegend.RegisterCallbacks(this, static (sender, _) => ((DoughnutGraph)sender).RefreshLegend());

        this._animationTimer = new DispatcherTimer { Interval = RenderInterval, };
        this._animationTimer.Tick += this.OnAnimationTick;

        this.Loaded += this.OnLoaded;
        this.Unloaded += this.OnUnloaded;
        this.ActualThemeChanged += this.OnActualThemeChanged;
        this.SizeChanged += (_, _) => this._legend?.UpdateLayout();
        this.UpdateEffectiveTrackColor();
    }

    private void SetRingThicknessCore(double value)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        lock (this._syncRoot)
        {
            this._ringThickness = value;
        }

        this.InvalidateCanvas();
    }

    private void SetGapAngleCore(double value)
    {
        if (!double.IsFinite(value) || value is < 0 or > 45)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        lock (this._syncRoot)
        {
            this._gapAngle = value;
        }

        this.InvalidateCanvas();
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

    /// <summary>
    /// Adds a slice. Configure every slice before the first call to <see cref="SetValues(double[])"/>.
    /// </summary>
    public void AddSeries(string name, Color color)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        lock (this._syncRoot)
        {
            if (this._hasBoundData || this._hasValues)
            {
                throw new InvalidOperationException("Series cannot be added after the first value update.");
            }

            if (this._series.Any(series => string.Equals(series.Name, name, StringComparison.Ordinal)))
            {
                throw new ArgumentException($"A series named '{name}' already exists.", nameof(name));
            }

            this._series.Add(new DoughnutGraphSeries(name, color));
            this._seriesSnapshot = this._series.ToArray();
            this._displayedValues = new double[this._series.Count];
            this._animationStartValues = new double[this._series.Count];
            this._animationTargetValues = new double[this._series.Count];
        }

        this.RefreshLegend();
        this.InvalidateCanvas();
    }

    /// <summary>
    /// Updates every slice using the stable order established by <see cref="AddSeries"/>.
    /// Values may use any common non-negative unit; the graph normalizes them to their sum.
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
                throw new InvalidOperationException("Add at least one series before setting values.");
            }

            if (valuesCopy.Length != this._series.Count)
            {
                throw new ArgumentException($"Expected {this._series.Count} values, but received {valuesCopy.Length}.", nameof(values));
            }

            this._hasValues = true;
            version = this._valueDispatcher.Version;
        }

        this._valueDispatcher.Submit(valuesCopy, version);
    }

    /// <summary>Transitions all slices to zero while preserving their configuration and center text.</summary>
    public void Clear()
    {
        double[] values;
        long version;
        lock (this._syncRoot)
        {
            if (this._hasBoundData)
            {
                throw new InvalidOperationException("Update the Data property when using a bound snapshot.");
            }

            if (this._series.Count == 0)
            {
                return;
            }

            values = new double[this._series.Count];
            this._hasValues = true;
            version = this._valueDispatcher.Version;
        }

        this._valueDispatcher.Submit(values, version);
    }

    private static void OnDataChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var chart = (DoughnutGraph)sender;
        var data = (DoughnutGraphData?)args.NewValue;
        var series = data?.Series ?? Array.Empty<DoughnutGraphSeries>();
        bool seriesChanged;
        lock (chart._syncRoot)
        {
            chart._valueDispatcher.Reset();
            chart._hasBoundData = data is not null;
            chart._hasValues = data is not null;
            seriesChanged = !chart._series.SequenceEqual(series);
            if (seriesChanged)
            {
                chart._series.Clear();
                chart._series.AddRange(series);
                chart._seriesSnapshot = chart._series.ToArray();
                chart._renderedValues = [];
                chart._isAnimating = false;
                chart._displayedValues = new double[series.Count];
            }
        }

        if (seriesChanged)
        {
            chart._inspector?.Clear();
        }

        chart.SetValuesCore(data?.ValueArray ?? []);
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new DoughnutGraphAutomationPeer(this);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        this._inspector?.Detach();
        this._inspector = null;
        this.DestroyCanvas();

        base.OnApplyTemplate();

        this._canvasHost = this.GetTemplateChild(CanvasHostPartName) as Grid;
        if (this._canvasHost is not null)
        {
            this._inspector = new ChartInspectorPresenter(
                this, this._canvasHost, this.GetTemplateChild(ChartInspectorPresenter.PartName) as Popup, () => this._renderedValues.Length,
                this.HitTestSlice, this.ReadSlice, readContext: this.ReadContext);
        }

        this._legend = new DoughnutLegendPresenter(this, this.GetTemplateChild(LegendPartName) as ScrollViewer, this.HighlightLegendSeries);
        this.UpdateShapeState();
        this.RefreshLegend();
        this.CreateCanvas();
    }

    private static void OnModeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var graph = (DoughnutGraph)sender;
        graph.UpdateShapeState();
        graph.InvalidateCanvas();
    }

    private static void OnShowLegendChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((DoughnutGraph)sender).RefreshLegend();
    }

    private void UpdateShapeState()
    {
        VisualStateManager.GoToState(this, this.Mode == DoughnutGraphMode.Pie ? "Pie" : "Doughnut", false);
    }

    private void RefreshLegend()
    {
        if (!this.DispatcherQueue.HasThreadAccess)
        {
            this.DispatcherQueue.TryEnqueue(this.RefreshLegend);
            return;
        }

        if (this._legend is null)
        {
            return;
        }

        DoughnutGraphSeries[] series;
        double[] shares;
        lock (this._syncRoot)
        {
            series = this._seriesSnapshot;
            shares = this._animationTargetValues;
        }

        this._legend.Update(series, shares, this._rawValues);
    }

    private void HighlightLegendSeries(int index)
    {
        if (this._renderer is not null)
        {
            this._renderer.HighlightedSeries = index;
            this._canvas?.Invalidate();
        }
    }

    private void SetValuesCore(double[] targetValues)
    {
        this._rawValues = targetValues;
        var now = this._clock.Now;
        var normalizedTargetValues = NormalizeValues(targetValues);
        bool shouldAnimate;

        lock (this._syncRoot)
        {
            var currentValues = this.GetDisplayedValuesCore(now);
            if (currentValues.Length != normalizedTargetValues.Length)
            {
                currentValues = new double[normalizedTargetValues.Length];
            }

            shouldAnimate = this.IsLoaded
                            && this._motion.AnimationsEnabled
                            && this._transitionDuration > TimeSpan.Zero
                            && !AreClose(currentValues, normalizedTargetValues);

            if (shouldAnimate)
            {
                this._displayedValues = currentValues;
                this._animationStartValues = currentValues;
                this._animationTargetValues = normalizedTargetValues;
                this._animationStartedAt = now;
                this._animationDuration = this._transitionDuration;
                this._isAnimating = true;
            }
            else
            {
                this._displayedValues = normalizedTargetValues;
                this._animationStartValues = normalizedTargetValues;
                this._animationTargetValues = normalizedTargetValues;
                this._isAnimating = false;
            }
        }

        if (shouldAnimate)
        {
            this._animationTimer.Start();
        }
        else
        {
            this._animationTimer.Stop();
        }

        this.RefreshLegend();
        this._canvas?.Invalidate();
    }

    private void OnAnimationTick(object? sender, object e)
    {
        var now = this._clock.Now;
        bool isAnimating;

        lock (this._syncRoot)
        {
            if (this._isAnimating && now - this._animationStartedAt >= this._animationDuration)
            {
                this._displayedValues = this._animationTargetValues;
                this._isAnimating = false;
            }

            isAnimating = this._isAnimating;
        }

        this._canvas?.Invalidate();
        if (!isAnimating)
        {
            this._animationTimer.Stop();
        }
    }

    private double[] GetDisplayedValuesCore(DateTimeOffset now)
    {
        if (!this._isAnimating || this._animationDuration == TimeSpan.Zero)
        {
            return this._displayedValues;
        }

        var progress = Math.Clamp((now - this._animationStartedAt).TotalMilliseconds / this._animationDuration.TotalMilliseconds, 0, 1);
        if (progress >= 1)
        {
            this._isAnimating = false;
            this._displayedValues = this._animationTargetValues;
            return this._displayedValues;
        }

        var easedProgress = (1 - Math.Cos(Math.PI * progress)) / 2;
        var values = new double[this._animationTargetValues.Length];
        for (var index = 0; index < values.Length; index++)
        {
            values[index] = this._animationStartValues[index]
                            + ((this._animationTargetValues[index] - this._animationStartValues[index]) * easedProgress);
        }

        return values;
    }

    private void OnCanvasDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        DoughnutGraphSeries[] series;
        double[] values;
        double ringThickness;
        double gapAngle;
        Color trackColor;

        lock (this._syncRoot)
        {
            series = this._seriesSnapshot;
            values = this.GetDisplayedValuesCore(this._clock.Now);
            ringThickness = this._ringThickness;
            gapAngle = this._gapAngle;
            trackColor = this._effectiveTrackColor;
        }

        this._renderer?.Draw(
            sender, args.DrawingSession, new Vector2((float)sender.ActualWidth, (float)sender.ActualHeight), this.Mode, series, values,
            ringThickness, gapAngle, trackColor);
        this._renderedValues = values;
        this._inspector?.Refresh();
    }

    private int HitTestSlice(Point point)
    {
        return DoughnutSliceRenderer.HitTest(
            new Vector2((float)point.X, (float)point.Y), this.GetPlotSize(), this.Mode, this._renderedValues, this.RingThickness,
            this.GapAngle);
    }

    private ChartInspectionItem? ReadSlice(int index)
    {
        var anchor = DoughnutSliceRenderer.GetAnchor(index, this.GetPlotSize(), this.Mode, this._renderedValues, this.RingThickness);
        if (anchor is not Vector2 position || index >= this._rawValues.Length)
        {
            return null;
        }

        string name;
        Color color;
        double share;
        lock (this._syncRoot)
        {
            name = this._series[index].Name;
            color = this._series[index].Color;
            share = this._animationTargetValues[index];
        }

        if (share == 0)
        {
            return null;
        }

        var raw = ChartInspectorPresenter.FormatValue(this._rawValues[index], this.ValueFormat);
        var percent = share.ToString("P1", CultureInfo.CurrentCulture);
        return new ChartInspectionItem(new Point(position.X, position.Y), $"{name}: {raw} ({percent})").WithSwatch(
            new SolidColorBrush(color));
    }

    private ChartContextValue[] ReadContext(int index)
    {
        lock (this._syncRoot)
        {
            var series = this._series[index];
            return [new ChartContextValue(series.Name, this._rawValues[index], series, index)];
        }
    }

    private Vector2 GetPlotSize()
    {
        return this._canvasHost is { } host ? new Vector2((float)host.ActualWidth, (float)host.ActualHeight) : Vector2.Zero;
    }

    private static double[] NormalizeValues(double[] values)
    {
        var normalizedValues = new double[values.Length];
        var maximum = values.Length == 0 ? 0 : values.Max();
        if (maximum <= 0)
        {
            return normalizedValues;
        }

        // Scaling before summing prevents otherwise-valid finite inputs from
        // overflowing when their raw total is greater than double.MaxValue.
        var scaledTotal = 0d;
        for (var index = 0; index < values.Length; index++)
        {
            scaledTotal += values[index] / maximum;
        }

        for (var index = 0; index < values.Length; index++)
        {
            normalizedValues[index] = (values[index] / maximum) / scaledTotal;
        }

        return normalizedValues;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        this.UpdateEffectiveTrackColor();
        this.CreateCanvas();

        lock (this._syncRoot)
        {
            if (this._isAnimating)
            {
                this._animationTimer.Start();
            }
        }

        this._canvas?.Invalidate();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        this.FinishAnimation();
        this.DestroyCanvas();
    }

    private void FinishAnimation()
    {
        this._animationTimer.Stop();
        lock (this._syncRoot)
        {
            if (this._isAnimating)
            {
                this._displayedValues = this._animationTargetValues;
                this._isAnimating = false;
            }
        }

        this._canvas?.Invalidate();
    }

    private void CreateCanvas()
    {
        if (this._canvas is not null || this._canvasHost is null)
        {
            return;
        }

        var canvas = new CanvasControl
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            ClearColor = Color.FromArgb(0, 0, 0, 0),
        };
        canvas.Draw += this.OnCanvasDraw;

        this._renderer = new DoughnutSliceRenderer();
        this._canvas = canvas;
        this._canvasHost.Children.Insert(0, canvas);
        canvas.Invalidate();
    }

    private void DestroyCanvas()
    {
        var canvas = this._canvas;
        if (canvas is null)
        {
            return;
        }

        this._canvas = null;
        this._renderedValues = [];
        canvas.Draw -= this.OnCanvasDraw;
        this._canvasHost?.Children.Remove(canvas);
        canvas.RemoveFromVisualTree();

        this._renderer?.Dispose();
        this._renderer = null;
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        this.UpdateEffectiveTrackColor();
        this._canvas?.Invalidate();
    }

    private void UpdateEffectiveTrackColor()
    {
        var color = this.ActualTheme == ElementTheme.Light ? Color.FromArgb(24, 0, 0, 0) : Color.FromArgb(28, 255, 255, 255);

        lock (this._syncRoot)
        {
            this._effectiveTrackColor = color;
        }
    }

    private void InvalidateCanvas()
    {
        if (this.DispatcherQueue.HasThreadAccess)
        {
            this._canvas?.Invalidate();
        }
        else
        {
            this.DispatcherQueue.TryEnqueue(() => this._canvas?.Invalidate());
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

    private sealed partial class DoughnutGraphAutomationPeer(DoughnutGraph owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore()
        {
            return nameof(DoughnutGraph);
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Group;
        }
    }
}
