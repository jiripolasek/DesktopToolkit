// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Compares categories on a common value axis using grouped or stacked bars.</summary>
[TemplatePart(Name = PlotPartName, Type = typeof(Canvas))]
[TemplatePart(Name = LegendPartName, Type = typeof(Grid))]
[TemplatePart(Name = DetailsPartName, Type = typeof(TextBlock))]
[TemplatePart(Name = ChartInspectorPresenter.PartName, Type = typeof(Popup))]
public sealed partial class BarChart : Control
{
    private const string PlotPartName = "PART_Plot";
    private const string LegendPartName = "PART_Legend";
    private const string DetailsPartName = "PART_Details";

    /// <summary>Identifies the <see cref="Data"/> dependency property.</summary>
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(BarChartData), typeof(BarChart), new PropertyMetadata(null, OnPlotPropertyChanged));

    /// <summary>Identifies the <see cref="Orientation"/> dependency property.</summary>
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation), typeof(Orientation), typeof(BarChart), new PropertyMetadata(Orientation.Vertical, OnPlotPropertyChanged));

    /// <summary>Identifies the <see cref="Mode"/> dependency property.</summary>
    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
        nameof(Mode), typeof(BarChartMode), typeof(BarChart), new PropertyMetadata(BarChartMode.Grouped, OnPlotPropertyChanged));

    /// <summary>Identifies the <see cref="BarSpacing"/> dependency property.</summary>
    public static readonly DependencyProperty BarSpacingProperty = DependencyProperty.Register(
        nameof(BarSpacing), typeof(double), typeof(BarChart), new PropertyMetadata(double.NaN, OnPlotPropertyChanged));

    /// <summary>Identifies the <see cref="SegmentGap"/> dependency property.</summary>
    public static readonly DependencyProperty SegmentGapProperty = DependencyProperty.Register(
        nameof(SegmentGap), typeof(double), typeof(BarChart), new PropertyMetadata(0d, OnPlotPropertyChanged));

    /// <summary>Identifies the <see cref="SegmentCornerRadius"/> dependency property.</summary>
    public static readonly DependencyProperty SegmentCornerRadiusProperty = DependencyProperty.Register(
        nameof(SegmentCornerRadius), typeof(double), typeof(BarChart), new PropertyMetadata(0d, OnPlotPropertyChanged));

    /// <summary>Identifies the <see cref="SegmentRounding"/> dependency property.</summary>
    public static readonly DependencyProperty SegmentRoundingProperty = DependencyProperty.Register(
        nameof(SegmentRounding), typeof(BarChartSegmentRounding), typeof(BarChart),
        new PropertyMetadata(BarChartSegmentRounding.OuterCorners, OnPlotPropertyChanged));

    /// <summary>Identifies the <see cref="InspectionMode"/> dependency property.</summary>
    public static readonly DependencyProperty InspectionModeProperty = DependencyProperty.Register(
        nameof(InspectionMode), typeof(BarChartInspectionMode), typeof(BarChart),
        new PropertyMetadata(BarChartInspectionMode.Bar, OnInspectionModeChanged));

    /// <summary>Identifies the <see cref="Minimum"/> dependency property.</summary>
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(double), typeof(BarChart), new PropertyMetadata(double.NaN, OnPlotPropertyChanged));

    /// <summary>Identifies the <see cref="Maximum"/> dependency property.</summary>
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(BarChart), new PropertyMetadata(double.NaN, OnPlotPropertyChanged));

    /// <summary>Identifies the <see cref="ValueFormat"/> dependency property.</summary>
    public static readonly DependencyProperty ValueFormatProperty = DependencyProperty.Register(
        nameof(ValueFormat), typeof(string), typeof(BarChart), new PropertyMetadata("G4", OnPlotPropertyChanged));

    /// <summary>Identifies the <see cref="ShowGrid"/> dependency property.</summary>
    public static readonly DependencyProperty ShowGridProperty = DependencyProperty.Register(
        nameof(ShowGrid), typeof(bool), typeof(BarChart), new PropertyMetadata(true, OnPlotPropertyChanged));

    /// <summary>Identifies the <see cref="CategoryAxis"/> dependency property.</summary>
    public static readonly DependencyProperty CategoryAxisProperty = DependencyProperty.Register(
        nameof(CategoryAxis), typeof(ChartAxisOptions), typeof(BarChart), new PropertyMetadata(null, OnPlotPropertyChanged));

    /// <summary>Identifies the <see cref="ValueAxis"/> dependency property.</summary>
    public static readonly DependencyProperty ValueAxisProperty = DependencyProperty.Register(
        nameof(ValueAxis), typeof(ChartAxisOptions), typeof(BarChart), new PropertyMetadata(null, OnPlotPropertyChanged));

    /// <summary>Identifies the <see cref="ShowLegend"/> dependency property.</summary>
    public static readonly DependencyProperty ShowLegendProperty = DependencyProperty.Register(
        nameof(ShowLegend), typeof(bool), typeof(BarChart), new PropertyMetadata(true, OnPlotPropertyChanged));

    private BarChartPresenter? _presenter;

    /// <summary>Gets or sets the category and series snapshot. Null clears the chart.</summary>
    public BarChartData? Data
    {
        get => (BarChartData?)this.GetValue(DataProperty);
        set => this.SetValue(DataProperty, value);
    }

    /// <summary>Gets or sets the direction in which bars extend.</summary>
    public Orientation Orientation
    {
        get => (Orientation)this.GetValue(OrientationProperty);
        set => this.SetValue(OrientationProperty, value);
    }

    /// <summary>Gets or sets how series are arranged within each category. Defaults to grouped.</summary>
    public BarChartMode Mode
    {
        get => (BarChartMode)this.GetValue(ModeProperty);
        set => this.SetValue(ModeProperty, value);
    }

    /// <summary>Gets or sets the spacing in DIPs between adjacent bars along the category axis.</summary>
    /// <remarks>Defaults to NaN for automatic spacing. A finite, non-negative value fills the plot with no outer gaps.</remarks>
    public double BarSpacing
    {
        get => (double)this.GetValue(BarSpacingProperty);
        set => this.SetValue(BarSpacingProperty, value);
    }

    /// <summary>Gets or sets the gap in DIPs between adjacent segments in stacked modes. Defaults to zero.</summary>
    public double SegmentGap
    {
        get => (double)this.GetValue(SegmentGapProperty);
        set => this.SetValue(SegmentGapProperty, value);
    }

    /// <summary>Gets or sets the maximum radius in DIPs for rounded stack corners. Zero gives square corners.</summary>
    /// <remarks>Applies to stacked and 100% stacked bars. The default is zero.</remarks>
    public double SegmentCornerRadius
    {
        get => (double)this.GetValue(SegmentCornerRadiusProperty);
        set => this.SetValue(SegmentCornerRadiusProperty, value);
    }

    /// <summary>Gets or sets which stacked bar corners use <see cref="SegmentCornerRadius"/>.</summary>
    /// <remarks>The default is <see cref="BarChartSegmentRounding.OuterCorners"/>, which leaves shared joins square.</remarks>
    public BarChartSegmentRounding SegmentRounding
    {
        get => (BarChartSegmentRounding)this.GetValue(SegmentRoundingProperty);
        set => this.SetValue(SegmentRoundingProperty, value);
    }

    /// <summary>Gets or sets which bars are compared in one tooltip. The default is an individual bar.</summary>
    public BarChartInspectionMode InspectionMode
    {
        get => (BarChartInspectionMode)this.GetValue(InspectionModeProperty);
        set => this.SetValue(InspectionModeProperty, value);
    }

    /// <summary>Gets or sets a nonpositive axis minimum, or NaN for automatic scaling. Ignored in 100% mode.</summary>
    public double Minimum
    {
        get => (double)this.GetValue(MinimumProperty);
        set => this.SetValue(MinimumProperty, value);
    }

    /// <summary>Gets or sets a nonnegative axis maximum, or NaN for automatic scaling. Ignored in 100% mode.</summary>
    public double Maximum
    {
        get => (double)this.GetValue(MaximumProperty);
        set => this.SetValue(MaximumProperty, value);
    }

    /// <summary>Gets or sets the format for raw values. A 100% axis and inspected shares use percentage formats.</summary>
    public string? ValueFormat
    {
        get => (string?)this.GetValue(ValueFormatProperty);
        set => this.SetValue(ValueFormatProperty, value);
    }

    /// <summary>Gets or sets whether to display grid lines in addition to the zero baseline.</summary>
    public bool ShowGrid
    {
        get => (bool)this.GetValue(ShowGridProperty);
        set => this.SetValue(ShowGridProperty, value);
    }

    /// <summary>Gets or sets visibility and opacity for category labels.</summary>
    public ChartAxisOptions? CategoryAxis
    {
        get => (ChartAxisOptions?)this.GetValue(CategoryAxisProperty);
        set => this.SetValue(CategoryAxisProperty, value);
    }

    /// <summary>Gets or sets value axis options. Null bounds use <see cref="Minimum"/> and <see cref="Maximum"/>.</summary>
    /// <remarks>In 100% mode, the scale and tick format are percentages.</remarks>
    public ChartAxisOptions? ValueAxis
    {
        get => (ChartAxisOptions?)this.GetValue(ValueAxisProperty);
        set => this.SetValue(ValueAxisProperty, value);
    }

    /// <summary>Gets or sets whether to display the series legend.</summary>
    public bool ShowLegend
    {
        get => (bool)this.GetValue(ShowLegendProperty);
        set => this.SetValue(ShowLegendProperty, value);
    }

    /// <summary>Initializes a grouped column chart with automatic axis bounds.</summary>
    public BarChart()
    {
        this.DefaultStyleKey = typeof(BarChart);
        this.RegisterPropertyChangedCallback(ChartPalette.ColorsProperty, this.OnAppearanceChanged);
        ChartAxisAppearance.RegisterCallbacks(this, this.OnAppearanceChanged);
        ChartLegend.RegisterCallbacks(this, this.OnAppearanceChanged);
        this.ActualThemeChanged += (_, _) => this._presenter?.Render();
        this.RegisterPropertyChangedCallback(ForegroundProperty, this.OnAppearanceChanged);
        this.RegisterPropertyChangedCallback(BorderBrushProperty, this.OnAppearanceChanged);
        this.RegisterPropertyChangedCallback(FontFamilyProperty, this.OnAppearanceChanged);
        this.RegisterPropertyChangedCallback(FontSizeProperty, this.OnAppearanceChanged);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        this._presenter?.Detach();
        this._presenter = null;
        base.OnApplyTemplate();

        if (this.GetTemplateChild(PlotPartName) is Canvas plot)
        {
            this._presenter = new BarChartPresenter(
                this, plot, this.GetTemplateChild(LegendPartName) as Grid, this.GetTemplateChild(ChartInspectorPresenter.PartName) as Popup,
                this.GetTemplateChild(DetailsPartName) as TextBlock);
            this._presenter.Render();
        }
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new BarChartAutomationPeer(this);
    }

    private static void OnPlotPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var presenter = ((BarChart)sender)._presenter;
        if (args.Property == ModeProperty)
        {
            presenter?.ClearInspection();
        }

        presenter?.Render();
    }

    private void OnAppearanceChanged(DependencyObject sender, DependencyProperty property)
    {
        this._presenter?.Render();
    }

    private static void OnInspectionModeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((BarChart)sender)._presenter?.UpdateInspectionMode();
    }

    private sealed partial class BarChartAutomationPeer(BarChart owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore()
        {
            return nameof(BarChart);
        }

        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrWhiteSpace(name) ? "Bar chart" : name;
        }
    }
}
