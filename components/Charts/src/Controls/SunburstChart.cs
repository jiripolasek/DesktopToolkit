// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Displays a weighted hierarchy as concentric rings.</summary>
[TemplatePart(Name = PlotPartName, Type = typeof(Canvas))]
[TemplatePart(Name = ChartInspectorPresenter.PartName, Type = typeof(Popup))]
public sealed partial class SunburstChart : Control
{
    /// <summary>Occurs when a segment is tapped or its keyboard inspection selection is activated with Enter.</summary>
    public event EventHandler<HierarchyNodeInvokedEventArgs>? NodeInvoked;

    private const string PlotPartName = "PART_Plot";
    private const double DefaultStartAngle = -90;

    /// <summary>Identifies the <see cref="Data"/> dependency property.</summary>
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(HierarchyChartData), typeof(SunburstChart), new PropertyMetadata(null, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="MaxDepth"/> dependency property.</summary>
    public static readonly DependencyProperty MaxDepthProperty = DependencyProperty.Register(
        nameof(MaxDepth), typeof(int), typeof(SunburstChart), new PropertyMetadata(3, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="StartAngle"/> dependency property.</summary>
    public static readonly DependencyProperty StartAngleProperty = DependencyProperty.Register(
        nameof(StartAngle), typeof(double), typeof(SunburstChart), new PropertyMetadata(DefaultStartAngle, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="ValueFormat"/> dependency property.</summary>
    public static readonly DependencyProperty ValueFormatProperty = DependencyProperty.Register(
        nameof(ValueFormat), typeof(string), typeof(SunburstChart), new PropertyMetadata("G6", OnChartPropertyChanged));

    private SunburstChartPresenter? _presenter;

    /// <summary>Gets or sets the hierarchy to plot. Null clears the chart.</summary>
    public HierarchyChartData? Data
    {
        get => (HierarchyChartData?)this.GetValue(DataProperty);
        set => this.SetValue(DataProperty, value);
    }

    /// <summary>Gets or sets the number of rings outside the root. The effective range is 1 through 32.</summary>
    public int MaxDepth
    {
        get => (int)this.GetValue(MaxDepthProperty);
        set => this.SetValue(MaxDepthProperty, value);
    }

    /// <summary>Gets or sets the start angle in degrees clockwise from the right. Defaults to -90.</summary>
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

    /// <summary>Gets or sets the numeric format for chart labels and inspection values.</summary>
    public string? ValueFormat
    {
        get => (string?)this.GetValue(ValueFormatProperty);
        set => this.SetValue(ValueFormatProperty, value);
    }

    internal double EffectiveStartAngle => double.IsFinite(this.StartAngle) ? this.StartAngle % 360 : DefaultStartAngle;

    /// <summary>Initializes a sunburst with its first segment at the top.</summary>
    public SunburstChart()
    {
        this.DefaultStyleKey = typeof(SunburstChart);
        this.RegisterPropertyChangedCallback(ChartPalette.ColorsProperty, this.OnAppearanceChanged);
        this.ActualThemeChanged += (_, _) => this._presenter?.Render();
        this.RegisterPropertyChangedCallback(ForegroundProperty, this.OnAppearanceChanged);
        this.RegisterPropertyChangedCallback(FontFamilyProperty, this.OnAppearanceChanged);
        this.RegisterPropertyChangedCallback(FontSizeProperty, this.OnAppearanceChanged);
        this.RegisterPropertyChangedCallback(FlowDirectionProperty, this.OnAppearanceChanged);
        this.RegisterPropertyChangedCallback(LanguageProperty, this.OnAppearanceChanged);
    }

    /// <summary>Tries to get the bounding rectangle of a visible segment by key, relative to this control in DIPs.</summary>
    public bool TryGetTileBounds(string key, out Rect bounds)
    {
        ArgumentNullException.ThrowIfNull(key);
        bounds = Rect.Empty;
        return this._presenter?.TryGetTileBounds(key, out bounds) == true;
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        this._presenter?.Detach();
        this._presenter = null;
        base.OnApplyTemplate();

        if (this.GetTemplateChild(PlotPartName) is Canvas plot)
        {
            this._presenter = new SunburstChartPresenter(
                this, plot, this.GetTemplateChild(ChartInspectorPresenter.PartName) as Popup, this.InvokeNode);
            this._presenter.Render();
        }
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new SunburstChartAutomationPeer(this);
    }

    private static void OnChartPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((SunburstChart)sender)._presenter?.Render();
    }

    private void OnAppearanceChanged(DependencyObject sender, DependencyProperty property)
    {
        this._presenter?.Render();
    }

    private bool InvokeNode(HierarchyChartNode node)
    {
        if (this.NodeInvoked is not { } handler)
        {
            return false;
        }

        var args = new HierarchyNodeInvokedEventArgs(node);
        handler(this, args);
        return args.Handled;
    }

    private sealed partial class SunburstChartAutomationPeer(SunburstChart owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore()
        {
            return nameof(SunburstChart);
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Group;
        }

        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrWhiteSpace(name) ? "Sunburst chart" : name;
        }
    }
}
