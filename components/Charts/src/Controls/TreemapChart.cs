// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Displays a weighted hierarchy as a set of nested rectangles.</summary>
[TemplatePart(Name = PlotPartName, Type = typeof(Canvas))]
[TemplatePart(Name = ChartInspectorPresenter.PartName, Type = typeof(Popup))]
public sealed partial class TreemapChart : Control
{
    /// <summary>Occurs when a tile is tapped or its keyboard inspection selection is activated with Enter.</summary>
    public event EventHandler<HierarchyNodeInvokedEventArgs>? NodeInvoked;

    private const string PlotPartName = "PART_Plot";
    private const double DefaultTileSpacing = 2;
    private const double DefaultBranchPadding = 8;
    private const double DefaultValueLabelOpacity = 0.83;

    /// <summary>Identifies the <see cref="Data"/> dependency property.</summary>
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(HierarchyChartData), typeof(TreemapChart), new PropertyMetadata(null, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="Layout"/> dependency property.</summary>
    public static readonly DependencyProperty LayoutProperty = DependencyProperty.Register(
        nameof(Layout), typeof(ITreemapLayout), typeof(TreemapChart), new PropertyMetadata(null, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="MaxDepth"/> dependency property.</summary>
    public static readonly DependencyProperty MaxDepthProperty = DependencyProperty.Register(
        nameof(MaxDepth), typeof(int), typeof(TreemapChart), new PropertyMetadata(3, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="TileSpacing"/> dependency property.</summary>
    public static readonly DependencyProperty TileSpacingProperty = DependencyProperty.Register(
        nameof(TileSpacing), typeof(double), typeof(TreemapChart), new PropertyMetadata(DefaultTileSpacing, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="BranchPadding"/> dependency property.</summary>
    public static readonly DependencyProperty BranchPaddingProperty = DependencyProperty.Register(
        nameof(BranchPadding), typeof(double), typeof(TreemapChart), new PropertyMetadata(DefaultBranchPadding, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="TileCornerRadius"/> dependency property.</summary>
    public static readonly DependencyProperty TileCornerRadiusProperty = DependencyProperty.Register(
        nameof(TileCornerRadius), typeof(double), typeof(TreemapChart), new PropertyMetadata(double.NaN, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="ShowValueLabels"/> dependency property.</summary>
    public static readonly DependencyProperty ShowValueLabelsProperty = DependencyProperty.Register(
        nameof(ShowValueLabels), typeof(bool), typeof(TreemapChart), new PropertyMetadata(true, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="ValueLabelOpacity"/> dependency property.</summary>
    public static readonly DependencyProperty ValueLabelOpacityProperty = DependencyProperty.Register(
        nameof(ValueLabelOpacity), typeof(double), typeof(TreemapChart),
        new PropertyMetadata(DefaultValueLabelOpacity, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="ValueFormat"/> dependency property.</summary>
    public static readonly DependencyProperty ValueFormatProperty = DependencyProperty.Register(
        nameof(ValueFormat), typeof(string), typeof(TreemapChart), new PropertyMetadata("G6", OnChartPropertyChanged));

    private HierarchyChartPresenter? _presenter;

    /// <summary>Gets or sets the hierarchy to plot. Null clears the chart.</summary>
    public HierarchyChartData? Data
    {
        get => (HierarchyChartData?)this.GetValue(DataProperty);
        set => this.SetValue(DataProperty, value);
    }

    /// <summary>Gets or sets the layout strategy. Null uses <see cref="TreemapLayouts.Squarified"/>.</summary>
    public ITreemapLayout? Layout
    {
        get => (ITreemapLayout?)this.GetValue(LayoutProperty);
        set => this.SetValue(LayoutProperty, value);
    }

    /// <summary>Gets or sets the maximum visible depth. The root's children start at level one.</summary>
    /// <remarks>Depth is limited to 1 through 32. Tiles need space for a header before they show children.</remarks>
    public int MaxDepth
    {
        get => (int)this.GetValue(MaxDepthProperty);
        set => this.SetValue(MaxDepthProperty, value);
    }

    /// <summary>Gets or sets the gap between adjacent tiles in DIPs. Defaults to 2.</summary>
    /// <remarks>Small tiles can use a reduced gap to keep their visible area.</remarks>
    public double TileSpacing
    {
        get => (double)this.GetValue(TileSpacingProperty);
        set => this.SetDimension(TileSpacingProperty, value);
    }

    /// <summary>Gets or sets the side and bottom inset of children in an expanded branch, in DIPs. Defaults to 8.</summary>
    /// <remarks>The branch header shares the side inset. Branches fold into a single tile when the children no longer fit.</remarks>
    public double BranchPadding
    {
        get => (double)this.GetValue(BranchPaddingProperty);
        set => this.SetDimension(BranchPaddingProperty, value);
    }

    /// <summary>Gets or sets the tile corner radius in DIPs. Use 0 for square corners or NaN for automatic rounding.</summary>
    /// <remarks>Automatic rounding uses 4 DIPs for leaves and 6 DIPs for expanded branches. Tile size and branch padding can limit the radius.</remarks>
    public double TileCornerRadius
    {
        get => (double)this.GetValue(TileCornerRadiusProperty);
        set => this.SetDimension(TileCornerRadiusProperty, value, allowAuto: true);
    }

    /// <summary>Gets or sets whether tiles display their sizes. The inspector still reports each value.</summary>
    public bool ShowValueLabels
    {
        get => (bool)this.GetValue(ShowValueLabelsProperty);
        set => this.SetValue(ShowValueLabelsProperty, value);
    }

    /// <summary>Gets or sets the size label opacity from 0 to 1. Defaults to 0.83. Contrast themes use full opacity.</summary>
    public double ValueLabelOpacity
    {
        get => (double)this.GetValue(ValueLabelOpacityProperty);
        set
        {
            if (value is not (>= 0 and <= 1))
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Label opacity must be between zero and one.");
            }

            this.SetValue(ValueLabelOpacityProperty, value);
        }
    }

    /// <summary>Gets or sets the numeric format for tile labels and inspection values.</summary>
    public string? ValueFormat
    {
        get => (string?)this.GetValue(ValueFormatProperty);
        set => this.SetValue(ValueFormatProperty, value);
    }

    internal double EffectiveBranchPadding => ResolveDimension(this.BranchPadding, DefaultBranchPadding);

    internal double EffectiveValueLabelOpacity =>
        this.ValueLabelOpacity is >= 0 and <= 1 ? this.ValueLabelOpacity : DefaultValueLabelOpacity;

    /// <summary>Initializes a treemap that displays up to three levels.</summary>
    public TreemapChart()
    {
        this.DefaultStyleKey = typeof(TreemapChart);
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
    protected override void OnApplyTemplate()
    {
        this._presenter?.Detach();
        this._presenter = null;
        base.OnApplyTemplate();

        if (this.GetTemplateChild(PlotPartName) is Canvas plot)
        {
            this._presenter = new HierarchyChartPresenter(
                this, plot, this.GetTemplateChild(ChartInspectorPresenter.PartName) as Popup, () => this.Data, () => this.ValueFormat,
                (root, bounds, labelHeight) => TreemapTileLayout.Create(
                    root, bounds, Math.Clamp(this.MaxDepth, 1, 32), labelHeight + TreemapTileLayout.HeaderSpacing,
                    ResolveDimension(this.TileSpacing, DefaultTileSpacing), this.EffectiveBranchPadding,
                    this.Layout ?? TreemapLayouts.Squarified), this.InvokeNode);
            this._presenter.Render();
        }
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new TreemapChartAutomationPeer(this);
    }

    internal double GetTileCornerRadius(bool expanded)
    {
        return ResolveDimension(this.TileCornerRadius, expanded ? 6 : 4);
    }

    private void SetDimension(DependencyProperty property, double value, bool allowAuto = false)
    {
        if ((value < 0 || !double.IsFinite(value)) && !(allowAuto && double.IsNaN(value)))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "The dimension must be finite and nonnegative.");
        }

        this.SetValue(property, value);
    }

    private static double ResolveDimension(double value, double fallback)
    {
        return double.IsFinite(value) && value >= 0 ? value : fallback;
    }

    private static void OnChartPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((TreemapChart)sender)._presenter?.Render();
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

    private sealed partial class TreemapChartAutomationPeer(TreemapChart owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore()
        {
            return nameof(TreemapChart);
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Group;
        }

        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrWhiteSpace(name) ? "Treemap chart" : name;
        }
    }
}
