// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Displays a range value on an arc with optional threshold bands.</summary>
[TemplatePart(Name = PlotPartName, Type = typeof(Canvas))]
[TemplatePart(Name = ValueLabelPartName, Type = typeof(TextBlock))]
[TemplatePart(Name = StatusLabelPartName, Type = typeof(TextBlock))]
[TemplatePart(Name = LabelsPartName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = ChartInspectorPresenter.PartName, Type = typeof(Popup))]
[TemplateVisualState(Name = "SemicircleArc", GroupName = "ArcStates")]
[TemplateVisualState(Name = "CustomArc", GroupName = "ArcStates")]
[TemplateVisualState(Name = "CenteredArc", GroupName = "ArcStates")]
public sealed partial class RadialGauge : RangeBase
{
    private const string PlotPartName = "PART_Plot";
    private const string ValueLabelPartName = "PART_ValueLabel";
    private const string StatusLabelPartName = "PART_StatusLabel";
    private const string LabelsPartName = "PART_Labels";
    private const string DefaultValueFormat = "0.#";
    private const double DefaultStartAngle = 180;
    private const double DefaultSweepAngle = 180;

    /// <summary>Identifies the <see cref="BandsSource"/> dependency property.</summary>
    public static readonly DependencyProperty BandsSourceProperty = DependencyProperty.Register(
        nameof(BandsSource), typeof(RadialGaugeBand[]), typeof(RadialGauge),
        new PropertyMetadata(null, static (sender, args) => ((RadialGauge)sender).ApplyBands((RadialGaugeBand[]?)args.NewValue ?? [])));

    /// <summary>Identifies the <see cref="IndicatorBrush"/> dependency property.</summary>
    public static readonly DependencyProperty IndicatorBrushProperty = DependencyProperty.Register(
        nameof(IndicatorBrush), typeof(Brush), typeof(RadialGauge), new PropertyMetadata(null, OnAppearanceChanged));

    /// <summary>Identifies the <see cref="ArcThickness"/> dependency property.</summary>
    public static readonly DependencyProperty ArcThicknessProperty = DependencyProperty.Register(
        nameof(ArcThickness), typeof(double), typeof(RadialGauge), new PropertyMetadata(double.NaN, OnAppearanceChanged));

    /// <summary>Identifies the <see cref="TrackBrush"/> dependency property.</summary>
    public static readonly DependencyProperty TrackBrushProperty = DependencyProperty.Register(
        nameof(TrackBrush), typeof(Brush), typeof(RadialGauge), new PropertyMetadata(null, OnAppearanceChanged));

    /// <summary>Identifies the <see cref="UseActiveBandColor"/> dependency property.</summary>
    public static readonly DependencyProperty UseActiveBandColorProperty = DependencyProperty.Register(
        nameof(UseActiveBandColor), typeof(bool), typeof(RadialGauge), new PropertyMetadata(false, OnAppearanceChanged));

    /// <summary>Identifies the <see cref="IsMirrored"/> dependency property.</summary>
    public static readonly DependencyProperty IsMirroredProperty = DependencyProperty.Register(
        nameof(IsMirrored), typeof(bool), typeof(RadialGauge), new PropertyMetadata(false, OnAppearanceChanged));

    /// <summary>Identifies the <see cref="IsDirectionReversed"/> dependency property.</summary>
    public static readonly DependencyProperty IsDirectionReversedProperty = DependencyProperty.Register(
        nameof(IsDirectionReversed), typeof(bool), typeof(RadialGauge), new PropertyMetadata(false, OnAppearanceChanged));

    /// <summary>Identifies the <see cref="StartAngle"/> dependency property.</summary>
    public static readonly DependencyProperty StartAngleProperty = DependencyProperty.Register(
        nameof(StartAngle), typeof(double), typeof(RadialGauge), new PropertyMetadata(DefaultStartAngle, OnArcChanged));

    /// <summary>Identifies the <see cref="SweepAngle"/> dependency property.</summary>
    public static readonly DependencyProperty SweepAngleProperty = DependencyProperty.Register(
        nameof(SweepAngle), typeof(double), typeof(RadialGauge), new PropertyMetadata(DefaultSweepAngle, OnArcChanged));

    /// <summary>Identifies the <see cref="IsValueLabelCentered"/> dependency property.</summary>
    public static readonly DependencyProperty IsValueLabelCenteredProperty = DependencyProperty.Register(
        nameof(IsValueLabelCentered), typeof(bool), typeof(RadialGauge), new PropertyMetadata(false, OnArcChanged));

    /// <summary>Identifies the <see cref="ValueFormat"/> dependency property.</summary>
    public static readonly DependencyProperty ValueFormatProperty = DependencyProperty.Register(
        nameof(ValueFormat), typeof(string), typeof(RadialGauge), new PropertyMetadata(DefaultValueFormat, OnFormatChanged));

    private IReadOnlyList<RadialGaugeBand> _bands = [];
    private RadialGaugePresenter? _presenter;
    private TextBlock? _valueLabel;
    private TextBlock? _statusLabel;
    private string _itemStatus = "0";

    /// <summary>Gets or sets threshold bands. Replace the array to update the chart.</summary>
    public RadialGaugeBand[]? BandsSource
    {
        get => (RadialGaugeBand[]?)this.GetValue(BandsSourceProperty);
        set => this.SetValue(BandsSourceProperty, value);
    }

    /// <summary>Gets or sets the requested track and indicator thickness in DIPs.</summary>
    /// <remarks>The renderer limits the thickness to the available size. NaN uses automatic sizing.</remarks>
    public double ArcThickness
    {
        get => (double)this.GetValue(ArcThicknessProperty);
        set => this.SetValue(ArcThicknessProperty, value);
    }

    /// <summary>Gets or sets the brush for the arc that indicates the current value.</summary>
    public Brush? IndicatorBrush
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(Brush))]
        get => (Brush?)this.GetValue(IndicatorBrushProperty);
        set => this.SetValue(IndicatorBrushProperty, value);
    }

    /// <summary>Gets or sets the brush for the unfilled track.</summary>
    public Brush? TrackBrush
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(Brush))]
        get => (Brush?)this.GetValue(TrackBrushProperty);
        set => this.SetValue(TrackBrushProperty, value);
    }

    /// <summary>Gets or sets whether the current value arc uses the matching threshold band's color.</summary>
    public bool UseActiveBandColor
    {
        get => (bool)this.GetValue(UseActiveBandColorProperty);
        set => this.SetValue(UseActiveBandColorProperty, value);
    }

    /// <summary>Gets or sets whether to reflect the arc horizontally after applying its angles.</summary>
    public bool IsMirrored
    {
        get => (bool)this.GetValue(IsMirroredProperty);
        set => this.SetValue(IsMirroredProperty, value);
    }

    /// <summary>Gets or sets whether values increase from the end of the configured arc toward its start.</summary>
    public bool IsDirectionReversed
    {
        get => (bool)this.GetValue(IsDirectionReversedProperty);
        set => this.SetValue(IsDirectionReversedProperty, value);
    }

    /// <summary>Gets or sets the arc's start in degrees clockwise from the right, before mirroring. Defaults to 180.</summary>
    public double StartAngle
    {
        get => (double)this.GetValue(StartAngleProperty);
        set
        {
            if (!double.IsFinite(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), "The start angle must be finite.");
            }

            this.SetValue(StartAngleProperty, value);
        }
    }

    /// <summary>Gets or sets the arc span in degrees, greater than zero and at most 360. Defaults to 180.</summary>
    public double SweepAngle
    {
        get => (double)this.GetValue(SweepAngleProperty);
        set
        {
            if (value is not (> 0 and <= 360))
            {
                throw new ArgumentOutOfRangeException(nameof(value), "The sweep angle must be greater than zero and at most 360.");
            }

            this.SetValue(SweepAngleProperty, value);
        }
    }

    /// <summary>Gets or sets whether to place the value and status labels at the geometric center of the arc.</summary>
    public bool IsValueLabelCentered
    {
        get => (bool)this.GetValue(IsValueLabelCenteredProperty);
        set => this.SetValue(IsValueLabelCenteredProperty, value);
    }

    /// <summary>Gets or sets the numeric format of the displayed and accessible value label.</summary>
    public string? ValueFormat
    {
        get => (string?)this.GetValue(ValueFormatProperty);
        set => this.SetValue(ValueFormatProperty, value);
    }

    /// <summary>Gets the configured threshold bands in ascending order.</summary>
    public IReadOnlyList<RadialGaugeBand> Bands => this._bands;

    internal double EffectiveStartAngle => double.IsFinite(this.StartAngle) ? ((this.StartAngle % 360) + 360) % 360 : DefaultStartAngle;

    internal double EffectiveSweepAngle => this.SweepAngle is > 0 and <= 360 ? this.SweepAngle : DefaultSweepAngle;

    internal bool UsesSemicircleLayout => this.EffectiveStartAngle == DefaultStartAngle && this.EffectiveSweepAngle == DefaultSweepAngle;

    /// <summary>Initializes a read-only gauge with the standard RangeBase range.</summary>
    public RadialGauge()
    {
        this.DefaultStyleKey = typeof(RadialGauge);
        WinUIResourceStrings.ObserveChanges(this, this.UpdateLabels);
        this.ActualThemeChanged += (_, _) => this._presenter?.Render();
        this.RegisterPropertyChangedCallback(ForegroundProperty, (_, _) => this._presenter?.Render());
    }

    /// <summary>
    /// Replaces the non-overlapping threshold bands on the UI thread.
    /// Pass no arguments to remove them. Bands are clipped to the gauge range.
    /// </summary>
    public void SetBands(params RadialGaugeBand[] bands)
    {
        ArgumentNullException.ThrowIfNull(bands);
        if (!this.DispatcherQueue.HasThreadAccess)
        {
            throw new InvalidOperationException("SetBands must be called from the UI thread.");
        }

        this.BandsSource = (RadialGaugeBand[])bands.Clone();
    }

    private void ApplyBands(RadialGaugeBand[] bands)
    {
        var snapshot = (RadialGaugeBand[])bands.Clone();
        if (Array.Exists(snapshot, band => band is null))
        {
            throw new ArgumentException("Bands cannot be null.", nameof(bands));
        }

        Array.Sort(snapshot, static (left, right) => left.Minimum.CompareTo(right.Minimum));
        for (var index = 1; index < snapshot.Length; index++)
        {
            if (snapshot[index].Minimum < snapshot[index - 1].Maximum)
            {
                throw new ArgumentException("Threshold bands cannot overlap.", nameof(bands));
            }
        }

        this._bands = Array.AsReadOnly(snapshot);
        this._presenter?.Render();
        this.UpdateLabels();
    }

    /// <inheritdoc />
    [WinRT.DynamicWindowsRuntimeCast(typeof(Canvas))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(Popup))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(TextBlock))]
    protected override void OnApplyTemplate()
    {
        this._presenter?.Detach();
        this._presenter = null;
        base.OnApplyTemplate();
        this.UpdateArcState();

        if (this.GetTemplateChild(PlotPartName) is Canvas plot)
        {
            this._presenter = new RadialGaugePresenter(
                this, plot, this.GetTemplateChild(ChartInspectorPresenter.PartName) as Popup,
                this.GetTemplateChild(LabelsPartName) as FrameworkElement);
            this._presenter.Render();
        }

        this._valueLabel = this.GetTemplateChild(ValueLabelPartName) as TextBlock;
        this._statusLabel = this.GetTemplateChild(StatusLabelPartName) as TextBlock;
        this.UpdateLabels();
    }

    /// <inheritdoc />
    protected override void OnValueChanged(double oldValue, double newValue)
    {
        base.OnValueChanged(oldValue, newValue);
        this._presenter?.UpdateValue();
        this.UpdateLabels();
        this.NotifyAutomation(RangeValuePatternIdentifiers.ValueProperty, oldValue, newValue);
    }

    /// <inheritdoc />
    protected override void OnMinimumChanged(double oldMinimum, double newMinimum)
    {
        base.OnMinimumChanged(oldMinimum, newMinimum);
        this._presenter?.Render();
        this.UpdateLabels();
        this.NotifyAutomation(RangeValuePatternIdentifiers.MinimumProperty, oldMinimum, newMinimum);
    }

    /// <inheritdoc />
    protected override void OnMaximumChanged(double oldMaximum, double newMaximum)
    {
        base.OnMaximumChanged(oldMaximum, newMaximum);
        this._presenter?.Render();
        this.UpdateLabels();
        this.NotifyAutomation(RangeValuePatternIdentifiers.MaximumProperty, oldMaximum, newMaximum);
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new RadialGaugeAutomationPeer(this);
    }

    internal string FormatValue(double value)
    {
        return ChartInspectorPresenter.FormatValue(value, this.ValueFormat ?? DefaultValueFormat, DefaultValueFormat);
    }

    internal RadialGaugeBand? GetActiveBand()
    {
        var value = this.Value;
        var minimum = this.Minimum;
        var maximum = this.Maximum;
        return maximum > minimum
            ? this._bands.FirstOrDefault(item =>
                item.Minimum < maximum
                && item.Maximum > minimum
                && value >= item.Minimum
                && (value < item.Maximum || (value == maximum && value == item.Maximum)))
            : null;
    }

    private void UpdateLabels()
    {
        var formatted = this.FormatValue(this.Value);
        var status = this.GetActiveBand()?.Label;
        if (this._valueLabel is not null)
        {
            this._valueLabel.Text = formatted;
        }

        if (this._statusLabel is not null)
        {
            this._statusLabel.Text = status ?? string.Empty;
            this._statusLabel.Visibility = string.IsNullOrWhiteSpace(status) ? Visibility.Collapsed : Visibility.Visible;
        }

        var previous = this._itemStatus;
        this._itemStatus = string.IsNullOrWhiteSpace(status)
            ? formatted
            : WinUIResourceStrings.FormatDefault("ListPairFormat", formatted, status);
        if (previous != this._itemStatus && string.IsNullOrEmpty(AutomationProperties.GetItemStatus(this)))
        {
            this.NotifyAutomation(AutomationElementIdentifiers.ItemStatusProperty, previous, this._itemStatus);
        }

        this._presenter?.RefreshInspection();
    }

    private void NotifyAutomation(AutomationProperty property, object oldValue, object newValue)
    {
        if (FrameworkElementAutomationPeer.FromElement(this) is { } peer)
        {
            peer.RaisePropertyChangedEvent(property, oldValue, newValue);
        }
    }

    private static void OnAppearanceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((RadialGauge)sender)._presenter?.Render();
    }

    private static void OnArcChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var gauge = (RadialGauge)sender;
        gauge.UpdateArcState();
        gauge._presenter?.Render();
    }

    private void UpdateArcState()
    {
        VisualStateManager.GoToState(
            this, this.IsValueLabelCentered ? "CenteredArc" : this.UsesSemicircleLayout ? "SemicircleArc" : "CustomArc", false);
    }

    private static void OnFormatChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((RadialGauge)sender).UpdateLabels();
    }

    private sealed partial class RadialGaugeAutomationPeer(RadialGauge owner) : FrameworkElementAutomationPeer(owner), IRangeValueProvider
    {
        bool IRangeValueProvider.IsReadOnly => true;

        double IRangeValueProvider.LargeChange => owner.LargeChange;

        double IRangeValueProvider.Maximum => owner.Maximum;

        double IRangeValueProvider.Minimum => owner.Minimum;

        double IRangeValueProvider.SmallChange => owner.SmallChange;

        double IRangeValueProvider.Value => owner.Value;

        void IRangeValueProvider.SetValue(double value)
        {
            throw new InvalidOperationException("RadialGauge is a read-only value indicator.");
        }

        protected override string GetClassNameCore()
        {
            return nameof(RadialGauge);
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.ProgressBar;
        }

        protected override object? GetPatternCore(PatternInterface patternInterface)
        {
            return patternInterface == PatternInterface.RangeValue ? this : base.GetPatternCore(patternInterface);
        }

        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrWhiteSpace(name) ? WinUIResourceStrings.Get("RadialGaugeName") : name;
        }

        protected override string GetItemStatusCore()
        {
            var status = base.GetItemStatusCore();
            return string.IsNullOrWhiteSpace(status) ? owner._itemStatus : status;
        }
    }
}
