// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Displays a matrix of numeric values as a grid of colored cells.</summary>
[TemplatePart(Name = "PART_Plot", Type = typeof(Canvas))]
[TemplatePart(Name = "PART_Legend", Type = typeof(Grid))]
[TemplatePart(Name = ChartInspectorPresenter.PartName, Type = typeof(Popup))]
public sealed partial class HeatmapChart : Control
{
    /// <summary>Identifies the <see cref="Data"/> dependency property.</summary>
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(HeatmapChartData), typeof(HeatmapChart), new PropertyMetadata(null, OnLayoutChanged));

    /// <summary>Identifies the <see cref="ColorScale"/> dependency property.</summary>
    public static readonly DependencyProperty ColorScaleProperty = DependencyProperty.Register(
        nameof(ColorScale), typeof(HeatmapColorScale), typeof(HeatmapChart), new PropertyMetadata(null, OnAppearanceChanged));

    /// <summary>Identifies the <see cref="KeepCellsSquare"/> dependency property.</summary>
    public static readonly DependencyProperty KeepCellsSquareProperty = DependencyProperty.Register(
        nameof(KeepCellsSquare), typeof(bool), typeof(HeatmapChart), new PropertyMetadata(false, OnLayoutChanged));

    /// <summary>Identifies the <see cref="CellSpacing"/> dependency property.</summary>
    public static readonly DependencyProperty CellSpacingProperty = DependencyProperty.Register(
        nameof(CellSpacing), typeof(double), typeof(HeatmapChart), new PropertyMetadata(2d, OnLayoutChanged));

    /// <summary>Identifies the <see cref="CellCornerRadius"/> dependency property.</summary>
    public static readonly DependencyProperty CellCornerRadiusProperty = DependencyProperty.Register(
        nameof(CellCornerRadius), typeof(double), typeof(HeatmapChart), new PropertyMetadata(2d, OnAppearanceChanged));

    /// <summary>Identifies the <see cref="ValueFormat"/> dependency property.</summary>
    public static readonly DependencyProperty ValueFormatProperty = DependencyProperty.Register(
        nameof(ValueFormat), typeof(string), typeof(HeatmapChart), new PropertyMetadata("G6", OnAppearanceChanged));

    /// <summary>Identifies the <see cref="ShowLegend"/> dependency property.</summary>
    public static readonly DependencyProperty ShowLegendProperty = DependencyProperty.Register(
        nameof(ShowLegend), typeof(bool), typeof(HeatmapChart), new PropertyMetadata(true, OnAppearanceChanged));

    /// <summary>Identifies the <see cref="RowAxis"/> dependency property.</summary>
    public static readonly DependencyProperty RowAxisProperty = DependencyProperty.Register(
        nameof(RowAxis), typeof(ChartAxisOptions), typeof(HeatmapChart), new PropertyMetadata(null, OnLayoutChanged));

    /// <summary>Identifies the <see cref="ColumnAxis"/> dependency property.</summary>
    public static readonly DependencyProperty ColumnAxisProperty = DependencyProperty.Register(
        nameof(ColumnAxis), typeof(ChartAxisOptions), typeof(HeatmapChart), new PropertyMetadata(null, OnLayoutChanged));

    private HeatmapChartPresenter? _presenter;

    /// <summary>Gets or sets the matrix snapshot. Null clears the chart.</summary>
    public HeatmapChartData? Data
    {
        get => (HeatmapChartData?)this.GetValue(DataProperty);
        set => this.SetValue(DataProperty, value);
    }

    /// <summary>Gets or sets the color scale. Null scales colors to the current data.</summary>
    public HeatmapColorScale? ColorScale
    {
        get => (HeatmapColorScale?)this.GetValue(ColorScaleProperty);
        set => this.SetValue(ColorScaleProperty, value);
    }

    /// <summary>Gets or sets whether cells have equal width and height.</summary>
    public bool KeepCellsSquare
    {
        get => (bool)this.GetValue(KeepCellsSquareProperty);
        set => this.SetValue(KeepCellsSquareProperty, value);
    }

    /// <summary>Gets or sets the spacing in effective pixels between cells.</summary>
    public double CellSpacing
    {
        get => (double)this.GetValue(CellSpacingProperty);
        set => this.SetValue(CellSpacingProperty, value);
    }

    /// <summary>Gets or sets the maximum corner radius in effective pixels.</summary>
    public double CellCornerRadius
    {
        get => (double)this.GetValue(CellCornerRadiusProperty);
        set => this.SetValue(CellCornerRadiusProperty, value);
    }

    /// <summary>Gets or sets the numeric format for inspected values and the color key.</summary>
    public string? ValueFormat
    {
        get => (string?)this.GetValue(ValueFormatProperty);
        set => this.SetValue(ValueFormatProperty, value);
    }

    /// <summary>Gets or sets whether to display a color key below the plot.</summary>
    public bool ShowLegend
    {
        get => (bool)this.GetValue(ShowLegendProperty);
        set => this.SetValue(ShowLegendProperty, value);
    }

    /// <summary>Gets or sets the visibility and opacity of the row labels.</summary>
    public ChartAxisOptions? RowAxis
    {
        get => (ChartAxisOptions?)this.GetValue(RowAxisProperty);
        set => this.SetValue(RowAxisProperty, value);
    }

    /// <summary>Gets or sets the visibility and opacity of the column labels.</summary>
    public ChartAxisOptions? ColumnAxis
    {
        get => (ChartAxisOptions?)this.GetValue(ColumnAxisProperty);
        set => this.SetValue(ColumnAxisProperty, value);
    }

    /// <summary>Initializes a heatmap with an automatic color scale.</summary>
    public HeatmapChart()
    {
        this.DefaultStyleKey = typeof(HeatmapChart);
        WinUIResourceStrings.ObserveChanges(this, this.RefreshLayout);
        ChartAxisAppearance.RegisterCallbacks(this, (_, _) => this.RefreshLayout());
        this.RegisterPropertyChangedCallback(ChartLegend.AlignmentProperty, (_, _) => this._presenter?.RefreshLegend());
        this.RegisterPropertyChangedCallback(ChartLegend.ItemTemplateProperty, (_, _) => this._presenter?.RefreshLegend());
        this.ActualThemeChanged += (_, _) => this._presenter?.UpdateAppearance();
        this.RegisterPropertyChangedCallback(ForegroundProperty, (_, _) => this.RefreshLayout());
        this.RegisterPropertyChangedCallback(FontFamilyProperty, (_, _) => this.RefreshLayout());
        this.RegisterPropertyChangedCallback(FontSizeProperty, (_, _) => this.RefreshLayout());
    }

    /// <inheritdoc />
    [WinRT.DynamicWindowsRuntimeCast(typeof(Canvas))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(Grid))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(Popup))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(TextBlock))]
    protected override void OnApplyTemplate()
    {
        this._presenter?.Detach();
        this._presenter = null;
        base.OnApplyTemplate();
        if (this.GetTemplateChild("PART_Plot") is Canvas plot)
        {
            this._presenter = new HeatmapChartPresenter(
                this, plot, this.GetTemplateChild("PART_Legend") as Grid, this.GetTemplateChild(ChartInspectorPresenter.PartName) as Popup,
                this.GetTemplateChild("PART_Details") as TextBlock);
            this.RefreshLayout();
        }
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new HeatmapAutomationPeer(this);
    }

    private static void OnLayoutChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var presenter = ((HeatmapChart)sender)._presenter;
        if (args.Property == DataProperty)
        {
            presenter?.ClearInspection();
        }

        ((HeatmapChart)sender).RefreshLayout();
    }

    private void RefreshLayout()
    {
        this._presenter?.RefreshLegend();
        this._presenter?.Render();
    }

    private static void OnAppearanceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((HeatmapChart)sender)._presenter?.UpdateAppearance();
    }

    private sealed partial class HeatmapAutomationPeer(HeatmapChart owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore()
        {
            return nameof(HeatmapChart);
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Group;
        }

        protected override string GetNameCore()
        {
            return base.GetNameCore() is { Length: > 0 } name ? name : WinUIResourceStrings.Get("HeatmapName");
        }
    }
}
