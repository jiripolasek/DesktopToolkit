// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Displays the levels of a weighted hierarchy in rows or columns.</summary>
[TemplatePart(Name = PlotPartName, Type = typeof(Canvas))]
[TemplatePart(Name = ChartInspectorPresenter.PartName, Type = typeof(Popup))]
public sealed partial class IcicleChart : Control
{
    /// <summary>Occurs when a tile is tapped or its keyboard inspection selection is activated with Enter.</summary>
    public event EventHandler<HierarchyNodeInvokedEventArgs>? NodeInvoked;

    private const string PlotPartName = "PART_Plot";

    /// <summary>Identifies the <see cref="Data"/> dependency property.</summary>
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(HierarchyChartData), typeof(IcicleChart), new PropertyMetadata(null, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="MaxDepth"/> dependency property.</summary>
    public static readonly DependencyProperty MaxDepthProperty = DependencyProperty.Register(
        nameof(MaxDepth), typeof(int), typeof(IcicleChart), new PropertyMetadata(3, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="Orientation"/> dependency property.</summary>
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation), typeof(Orientation), typeof(IcicleChart), new PropertyMetadata(Orientation.Vertical, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="ValueFormat"/> dependency property.</summary>
    public static readonly DependencyProperty ValueFormatProperty = DependencyProperty.Register(
        nameof(ValueFormat), typeof(string), typeof(IcicleChart), new PropertyMetadata("G6", OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="ShowValueLabels"/> dependency property.</summary>
    public static readonly DependencyProperty ShowValueLabelsProperty = DependencyProperty.Register(
        nameof(ShowValueLabels), typeof(bool), typeof(IcicleChart), new PropertyMetadata(true, OnChartPropertyChanged));

    private HierarchyChartPresenter? _presenter;

    /// <summary>Gets or sets whether space permitting, tiles display their values below their names.</summary>
    public bool ShowValueLabels
    {
        get => (bool)this.GetValue(ShowValueLabelsProperty);
        set => this.SetValue(ShowValueLabelsProperty, value);
    }

    /// <summary>Gets or sets the hierarchy to plot. Null clears the chart.</summary>
    public HierarchyChartData? Data
    {
        get => (HierarchyChartData?)this.GetValue(DataProperty);
        set => this.SetValue(DataProperty, value);
    }

    /// <summary>Gets or sets the number of levels below the root to display. The effective range is 1 through 32.</summary>
    public int MaxDepth
    {
        get => (int)this.GetValue(MaxDepthProperty);
        set => this.SetValue(MaxDepthProperty, value);
    }

    /// <summary>Gets or sets the direction in which levels grow from the root.</summary>
    public Orientation Orientation
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(Orientation))]
        get => (Orientation)this.GetValue(OrientationProperty);
        set => this.SetValue(OrientationProperty, value);
    }

    /// <summary>Gets or sets the numeric format for tile labels and inspection values.</summary>
    public string? ValueFormat
    {
        get => (string?)this.GetValue(ValueFormatProperty);
        set => this.SetValue(ValueFormatProperty, value);
    }

    /// <summary>Initializes a top-to-bottom icicle chart.</summary>
    public IcicleChart()
    {
        this.DefaultStyleKey = typeof(IcicleChart);
        WinUIResourceStrings.ObserveChanges(this, () => this._presenter?.Render());
        this.RegisterPropertyChangedCallback(ChartPalette.ColorsProperty, this.OnAppearanceChanged);
        this.ActualThemeChanged += (_, _) => this._presenter?.Render();
        this.RegisterPropertyChangedCallback(ForegroundProperty, this.OnAppearanceChanged);
        this.RegisterPropertyChangedCallback(FontFamilyProperty, this.OnAppearanceChanged);
        this.RegisterPropertyChangedCallback(FontSizeProperty, this.OnAppearanceChanged);
    }

    /// <summary>Tries to get the visible tile bounds for a key, relative to this control in DIPs.</summary>
    public bool TryGetTileBounds(string key, out Rect bounds)
    {
        ArgumentNullException.ThrowIfNull(key);
        bounds = Rect.Empty;
        return this._presenter?.TryGetTileBounds(key, out bounds) == true;
    }

    /// <inheritdoc />
    [WinRT.DynamicWindowsRuntimeCast(typeof(Canvas))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(Popup))]
    protected override void OnApplyTemplate()
    {
        this._presenter?.Detach();
        this._presenter = null;
        base.OnApplyTemplate();

        if (this.GetTemplateChild(PlotPartName) is Canvas plot)
        {
            this._presenter = new HierarchyChartPresenter(
                this, plot, this.GetTemplateChild(ChartInspectorPresenter.PartName) as Popup, () => this.Data, () => this.ValueFormat,
                (root, bounds, _) => IcicleTileLayout.Create(root, bounds, Math.Clamp(this.MaxDepth, 1, 32), this.Orientation),
                this.InvokeNode);
            this._presenter.Render();
        }
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new IcicleChartAutomationPeer(this);
    }

    private static void OnChartPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((IcicleChart)sender)._presenter?.Render();
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

    private sealed partial class IcicleChartAutomationPeer(IcicleChart owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore()
        {
            return nameof(IcicleChart);
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Group;
        }

        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrWhiteSpace(name) ? WinUIResourceStrings.Get("IcicleChartName") : name;
        }
    }
}
