// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Compares several numeric dimensions on radial spokes.</summary>
[TemplatePart(Name = "PART_Plot", Type = typeof(Canvas))]
[TemplatePart(Name = "PART_Legend", Type = typeof(Grid))]
[TemplatePart(Name = ChartInspectorPresenter.PartName, Type = typeof(Popup))]
public sealed partial class SpiderChart : Control
{
    /// <summary>Identifies the <see cref="Data"/> dependency property.</summary>
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(SpiderChartData), typeof(SpiderChart), new PropertyMetadata(null, OnChartChanged));

    /// <summary>Identifies the <see cref="RingCount"/> dependency property.</summary>
    public static readonly DependencyProperty RingCountProperty = DependencyProperty.Register(
        nameof(RingCount), typeof(int), typeof(SpiderChart), new PropertyMetadata(4, OnChartChanged));

    /// <summary>Identifies the <see cref="ShowAreaFill"/> dependency property.</summary>
    public static readonly DependencyProperty ShowAreaFillProperty = DependencyProperty.Register(
        nameof(ShowAreaFill), typeof(bool), typeof(SpiderChart), new PropertyMetadata(true, OnChartChanged));

    /// <summary>Identifies the <see cref="ShowLegend"/> dependency property.</summary>
    public static readonly DependencyProperty ShowLegendProperty = DependencyProperty.Register(
        nameof(ShowLegend), typeof(bool), typeof(SpiderChart), new PropertyMetadata(true, OnChartChanged));

    /// <summary>Identifies the <see cref="AxisOptions"/> dependency property.</summary>
    public static readonly DependencyProperty AxisOptionsProperty = DependencyProperty.Register(
        nameof(AxisOptions), typeof(ChartAxisOptions), typeof(SpiderChart), new PropertyMetadata(null, OnChartChanged));

    private SpiderChartPresenter? _presenter;

    /// <summary>Gets or sets the axis and series snapshot. Null clears the chart.</summary>
    public SpiderChartData? Data
    {
        get => (SpiderChartData?)this.GetValue(DataProperty);
        set => this.SetValue(DataProperty, value);
    }

    /// <summary>Gets or sets the number of grid rings, from 1 through 12.</summary>
    public int RingCount
    {
        get => (int)this.GetValue(RingCountProperty);
        set => this.SetValue(RingCountProperty, value);
    }

    /// <summary>Gets or sets whether to fill complete polygons.</summary>
    public bool ShowAreaFill
    {
        get => (bool)this.GetValue(ShowAreaFillProperty);
        set => this.SetValue(ShowAreaFillProperty, value);
    }

    /// <summary>Gets or sets whether to show the series legend.</summary>
    public bool ShowLegend
    {
        get => (bool)this.GetValue(ShowLegendProperty);
        set => this.SetValue(ShowLegendProperty, value);
    }

    /// <summary>Gets or sets presentation options for the spokes, grid rings, and axis labels.</summary>
    public ChartAxisOptions? AxisOptions
    {
        get => (ChartAxisOptions?)this.GetValue(AxisOptionsProperty);
        set => this.SetValue(AxisOptionsProperty, value);
    }

    /// <summary>Initializes a spider chart with optional filled polygons and a legend.</summary>
    public SpiderChart()
    {
        this.DefaultStyleKey = typeof(SpiderChart);
        ChartAxisAppearance.RegisterCallbacks(this, (_, _) => this._presenter?.Render());
        ChartLegend.RegisterCallbacks(this, (_, _) => this._presenter?.Render());
        this.RegisterPropertyChangedCallback(ChartPalette.ColorsProperty, (_, _) => this._presenter?.Render());
        this.ActualThemeChanged += (_, _) => this._presenter?.Render();
        this.RegisterPropertyChangedCallback(ForegroundProperty, (_, _) => this._presenter?.Render());
        this.RegisterPropertyChangedCallback(BorderBrushProperty, (_, _) => this._presenter?.Render());
        this.RegisterPropertyChangedCallback(FontSizeProperty, (_, _) => this._presenter?.Render());
        this.RegisterPropertyChangedCallback(FontFamilyProperty, (_, _) => this._presenter?.Render());
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        this._presenter?.Detach();
        this._presenter = null;
        base.OnApplyTemplate();
        if (this.GetTemplateChild("PART_Plot") is Canvas plot)
        {
            this._presenter = new SpiderChartPresenter(
                this, plot, this.GetTemplateChild("PART_Legend") as Grid, this.GetTemplateChild(ChartInspectorPresenter.PartName) as Popup,
                this.GetTemplateChild("PART_Details") as TextBlock);
            this._presenter.Render();
        }
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new SpiderAutomationPeer(this);
    }

    private static void OnChartChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var presenter = ((SpiderChart)sender)._presenter;
        if (args.Property == DataProperty)
        {
            presenter?.ClearInspection();
        }

        presenter?.Render();
    }

    private sealed partial class SpiderAutomationPeer(SpiderChart owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore()
        {
            return nameof(SpiderChart);
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Group;
        }

        protected override string GetNameCore()
        {
            return base.GetNameCore() is { Length: > 0 } name ? name : "Spider chart";
        }
    }
}
