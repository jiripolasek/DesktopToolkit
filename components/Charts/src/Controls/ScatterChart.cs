// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Plots points on numeric X and Y axes.</summary>
[TemplatePart(Name = PlotPartName, Type = typeof(Canvas))]
[TemplatePart(Name = LegendPartName, Type = typeof(Grid))]
[TemplatePart(Name = DetailsPartName, Type = typeof(TextBlock))]
[TemplatePart(Name = ChartInspectorPresenter.PartName, Type = typeof(Popup))]
public sealed partial class ScatterChart : Control
{
    private const string PlotPartName = "PART_Plot";
    private const string LegendPartName = "PART_Legend";
    private const string DetailsPartName = "PART_Details";

    /// <summary>Identifies the <see cref="Data"/> dependency property.</summary>
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(ScatterChartData), typeof(ScatterChart), new PropertyMetadata(null, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="MarkerSize"/> dependency property.</summary>
    public static readonly DependencyProperty MarkerSizeProperty = DependencyProperty.Register(
        nameof(MarkerSize), typeof(double), typeof(ScatterChart), new PropertyMetadata(8d, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="XValueFormat"/> dependency property.</summary>
    public static readonly DependencyProperty XValueFormatProperty = DependencyProperty.Register(
        nameof(XValueFormat), typeof(string), typeof(ScatterChart), new PropertyMetadata("G6", OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="YValueFormat"/> dependency property.</summary>
    public static readonly DependencyProperty YValueFormatProperty = DependencyProperty.Register(
        nameof(YValueFormat), typeof(string), typeof(ScatterChart), new PropertyMetadata("G6", OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="ShowGrid"/> dependency property.</summary>
    public static readonly DependencyProperty ShowGridProperty = DependencyProperty.Register(
        nameof(ShowGrid), typeof(bool), typeof(ScatterChart), new PropertyMetadata(true, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="ShowLegend"/> dependency property.</summary>
    public static readonly DependencyProperty ShowLegendProperty = DependencyProperty.Register(
        nameof(ShowLegend), typeof(bool), typeof(ScatterChart), new PropertyMetadata(true, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="XAxis"/> dependency property.</summary>
    public static readonly DependencyProperty XAxisProperty = DependencyProperty.Register(
        nameof(XAxis), typeof(ChartAxisOptions), typeof(ScatterChart), new PropertyMetadata(null, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="Milestones"/> dependency property.</summary>
    public static readonly DependencyProperty MilestonesProperty = DependencyProperty.Register(
        nameof(Milestones), typeof(ChartMilestones), typeof(ScatterChart), new PropertyMetadata(null, OnChartPropertyChanged));

    /// <summary>Identifies the <see cref="YAxis"/> dependency property.</summary>
    public static readonly DependencyProperty YAxisProperty = DependencyProperty.Register(
        nameof(YAxis), typeof(ChartAxisOptions), typeof(ScatterChart), new PropertyMetadata(null, OnChartPropertyChanged));

    private ScatterChartPresenter? _presenter;

    /// <summary>Gets or sets the X-axis settings. Null uses automatic settings.</summary>
    public ChartAxisOptions? XAxis
    {
        get => (ChartAxisOptions?)this.GetValue(XAxisProperty);
        set => this.SetValue(XAxisProperty, value);
    }

    /// <summary>Gets or sets the milestone snapshot. Null clears the markers.</summary>
    public ChartMilestones? Milestones
    {
        get => (ChartMilestones?)this.GetValue(MilestonesProperty);
        set => this.SetValue(MilestonesProperty, value);
    }

    /// <summary>Gets or sets the Y-axis settings. Null uses automatic settings.</summary>
    public ChartAxisOptions? YAxis
    {
        get => (ChartAxisOptions?)this.GetValue(YAxisProperty);
        set => this.SetValue(YAxisProperty, value);
    }

    /// <summary>Gets or sets the series snapshot. Null clears the chart.</summary>
    public ScatterChartData? Data
    {
        get => (ScatterChartData?)this.GetValue(DataProperty);
        set => this.SetValue(DataProperty, value);
    }

    /// <summary>Gets or sets the marker diameter in effective pixels. Values are limited to 1 through 48. Defaults to 8.</summary>
    public double MarkerSize
    {
        get => (double)this.GetValue(MarkerSizeProperty);
        set => this.SetValue(MarkerSizeProperty, value);
    }

    /// <summary>Gets or sets the format for X-axis labels and inspected X values.</summary>
    public string? XValueFormat
    {
        get => (string?)this.GetValue(XValueFormatProperty);
        set => this.SetValue(XValueFormatProperty, value);
    }

    /// <summary>Gets or sets the format for Y-axis labels and inspected Y values.</summary>
    public string? YValueFormat
    {
        get => (string?)this.GetValue(YValueFormatProperty);
        set => this.SetValue(YValueFormatProperty, value);
    }

    /// <summary>Gets or sets whether to show the rectangular grid.</summary>
    public bool ShowGrid
    {
        get => (bool)this.GetValue(ShowGridProperty);
        set => this.SetValue(ShowGridProperty, value);
    }

    /// <summary>Gets or sets whether to show the series legend.</summary>
    public bool ShowLegend
    {
        get => (bool)this.GetValue(ShowLegendProperty);
        set => this.SetValue(ShowLegendProperty, value);
    }

    /// <summary>Initializes a scatter chart with automatic axis bounds and a legend.</summary>
    public ScatterChart()
    {
        this.DefaultStyleKey = typeof(ScatterChart);
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
            this._presenter = new ScatterChartPresenter(
                this, plot, this.GetTemplateChild(LegendPartName) as Grid, this.GetTemplateChild(ChartInspectorPresenter.PartName) as Popup,
                this.GetTemplateChild(DetailsPartName) as TextBlock);
            this._presenter.Render();
        }
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new ScatterChartAutomationPeer(this);
    }

    private static void OnChartPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var presenter = ((ScatterChart)sender)._presenter;
        if (args.Property == DataProperty || args.Property == XAxisProperty || args.Property == YAxisProperty)
        {
            presenter?.ClearInspection();
        }

        presenter?.Render();
    }

    private void OnAppearanceChanged(DependencyObject sender, DependencyProperty property)
    {
        this._presenter?.Render();
    }

    private sealed partial class ScatterChartAutomationPeer(ScatterChart owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore()
        {
            return nameof(ScatterChart);
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Group;
        }

        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrWhiteSpace(name) ? "Scatter chart" : name;
        }
    }
}
