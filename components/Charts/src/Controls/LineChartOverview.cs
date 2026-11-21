// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Shows the full trace of a line chart and lets users move, resize, and zoom its visible X window.</summary>
[TemplatePart(Name = TrackPart, Type = typeof(Canvas))]
public sealed partial class LineChartOverview : Control
{
    private const string TrackPart = "PART_Track";
    internal const double SmallPanFraction = 0.1;
    internal const double LargePanFraction = 0.9;

    /// <summary>Identifies the <see cref="Target"/> dependency property.</summary>
    public static readonly DependencyProperty TargetProperty = DependencyProperty.Register(
        nameof(Target), typeof(LineChart), typeof(LineChartOverview), new PropertyMetadata(null, OnTargetChanged));

    private LineChartOverviewPresenter? _presenter;

    /// <summary>Gets or sets the line chart shown and navigated by the overview.</summary>
    /// <remarks>Null clears the overview. A linked target shares overview navigation with its viewport group.</remarks>
    public LineChart? Target
    {
        get => (LineChart?)this.GetValue(TargetProperty);
        set => this.SetValue(TargetProperty, value);
    }

    internal bool CanPan
    {
        get
        {
            var (start, end) = this.GetWindow();
            return this.IsEnabled && this.Target is { IsEnabled: true } && end - start is > 0 and < 1;
        }
    }

    internal bool CanResize => this.IsEnabled && this.Target is { IsEnabled: true, NavigationExtent: not null };

    /// <summary>Initializes an overview that can be attached to a line chart.</summary>
    public LineChartOverview()
    {
        this.DefaultStyleKey = typeof(LineChartOverview);
        this.Loaded += (_, _) => this._presenter?.Connect(this.Target);
        this.Unloaded += (_, _) => this._presenter?.Connect(null);
        this.ActualThemeChanged += (_, _) => this._presenter?.Render();
        this.RegisterPropertyChangedCallback(ForegroundProperty, (_, _) => this._presenter?.RefreshWindow());
        this.IsEnabledChanged += (_, _) =>
        {
            this._presenter?.CancelDrag();
            this._presenter?.RefreshWindow();
        };
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        this._presenter?.Detach();
        this._presenter = null;
        base.OnApplyTemplate();
        if (this.GetTemplateChild(TrackPart) is Canvas track)
        {
            this._presenter = new LineChartOverviewPresenter(this, track);
            if (this.IsLoaded)
            {
                this._presenter.Connect(this.Target);
            }
        }
    }

    internal (double Start, double End) GetWindow()
    {
        if (this.Target?.NavigationExtent is not { } extent)
        {
            return default;
        }

        return this.Target.VisibleViewport is { } view ? extent.GetFractions(view) : (0, 1);
    }

    internal bool PanTo(double start)
    {
        var (current, end) = this.GetWindow();
        var width = end - current;
        return this.IsEnabled
               && this.Target is { IsEnabled: true } target
               && width is > 0 and < 1
               && target.PanBy((Math.Clamp(start, 0, 1 - width) - current) / width);
    }

    internal bool SetWindow(double start, double end)
    {
        return this.CanResize && this.Target!.SetNavigationWindow(Math.Clamp(start, 0, 1), Math.Clamp(end, 0, 1));
    }

    internal void ZoomAt(double factor, double position)
    {
        var (first, last) = this.GetWindow();
        var width = last - first;
        if (!this.CanResize || width <= 0)
        {
            return;
        }

        var relativeAnchor = position >= first && position <= last ? (position - first) / width : 0.5;
        var nextWidth = Math.Min(1, width / factor);
        var nextStart = Math.Clamp(position - nextWidth * relativeAnchor, 0, 1 - nextWidth);
        this.SetWindow(nextStart, nextStart + nextWidth);
    }

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

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new OverviewAutomationPeer(this);
    }

    private static void OnTargetChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is LineChartOverview { IsLoaded: true } overview)
        {
            overview._presenter?.Connect((LineChart?)args.NewValue);
        }
    }

    private sealed partial class OverviewAutomationPeer(LineChartOverview owner)
        : FrameworkElementAutomationPeer(owner), IRangeValueProvider
    {
        public bool IsReadOnly => !owner.CanPan;

        public double Minimum => 0;

        public double Maximum => 100;

        public double SmallChange => this.GetChange(SmallPanFraction);

        public double LargeChange => this.GetChange(LargePanFraction);

        public double Value
        {
            get
            {
                var (start, end) = owner.GetWindow();
                var travel = 1 - (end - start);
                return travel > 0 ? Math.Clamp(start / travel * this.Maximum, this.Minimum, this.Maximum) : 0;
            }
        }

        public void SetValue(double value)
        {
            if (this.IsReadOnly)
            {
                throw new InvalidOperationException("Zoom the chart before scrolling.");
            }

            if (!double.IsFinite(value) || value < this.Minimum || value > this.Maximum)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            var (start, end) = owner.GetWindow();
            owner.PanTo((1 - (end - start)) * value / this.Maximum);
        }

        protected override string GetClassNameCore()
        {
            return nameof(LineChartOverview);
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.ScrollBar;
        }

        protected override string GetNameCore()
        {
            return base.GetNameCore() is { Length: > 0 } name ? name : "Chart overview";
        }

        protected override object? GetPatternCore(PatternInterface patternInterface)
        {
            return patternInterface == PatternInterface.RangeValue ? this : base.GetPatternCore(patternInterface);
        }

        private double GetChange(double windowFraction)
        {
            var (start, end) = owner.GetWindow();
            var width = end - start;
            return width is > 0 and < 1 ? Math.Min(this.Maximum, this.Maximum * width * windowFraction / (1 - width)) : 0;
        }
    }
}
