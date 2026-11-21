// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Compares ordered stages as bands or connected ribbons proportional to their values.</summary>
[TemplatePart(Name = "PART_Plot", Type = typeof(Canvas))]
[TemplatePart(Name = "PART_Legend", Type = typeof(Grid))]
[TemplatePart(Name = ChartInspectorPresenter.PartName, Type = typeof(Popup))]
public sealed partial class FunnelChart : Control
{
    /// <summary>Identifies the <see cref="Data"/> dependency property.</summary>
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(FunnelChartData), typeof(FunnelChart), new PropertyMetadata(null, OnChartChanged));

    /// <summary>Identifies the <see cref="Presentation"/> dependency property.</summary>
    public static readonly DependencyProperty PresentationProperty = DependencyProperty.Register(
        nameof(Presentation), typeof(FunnelChartPresentation), typeof(FunnelChart),
        new PropertyMetadata(FunnelChartPresentation.Stages, OnChartChanged));

    /// <summary>Identifies the <see cref="StageAlignment"/> dependency property.</summary>
    public static readonly DependencyProperty StageAlignmentProperty = DependencyProperty.Register(
        nameof(StageAlignment), typeof(HorizontalAlignment), typeof(FunnelChart),
        new PropertyMetadata(HorizontalAlignment.Center, OnChartChanged));

    /// <summary>Identifies the <see cref="ShowLegend"/> dependency property.</summary>
    public static readonly DependencyProperty ShowLegendProperty = DependencyProperty.Register(
        nameof(ShowLegend), typeof(bool), typeof(FunnelChart), new PropertyMetadata(true, OnChartChanged));

    /// <summary>Identifies the <see cref="IsTapered"/> dependency property.</summary>
    public static readonly DependencyProperty IsTaperedProperty = DependencyProperty.Register(
        nameof(IsTapered), typeof(bool), typeof(FunnelChart), new PropertyMetadata(true, OnChartChanged));

    /// <summary>Identifies the <see cref="StageSpacing"/> dependency property.</summary>
    public static readonly DependencyProperty StageSpacingProperty = DependencyProperty.Register(
        nameof(StageSpacing), typeof(double), typeof(FunnelChart), new PropertyMetadata(6d, OnChartChanged));

    /// <summary>Identifies the <see cref="StageCornerRadius"/> dependency property.</summary>
    public static readonly DependencyProperty StageCornerRadiusProperty = DependencyProperty.Register(
        nameof(StageCornerRadius), typeof(double), typeof(FunnelChart), new PropertyMetadata(0d, OnChartChanged));

    /// <summary>Identifies the <see cref="ShowLabels"/> dependency property.</summary>
    public static readonly DependencyProperty ShowLabelsProperty = DependencyProperty.Register(
        nameof(ShowLabels), typeof(bool), typeof(FunnelChart), new PropertyMetadata(true, OnChartChanged));

    /// <summary>Identifies the <see cref="ValueFormat"/> dependency property.</summary>
    public static readonly DependencyProperty ValueFormatProperty = DependencyProperty.Register(
        nameof(ValueFormat), typeof(string), typeof(FunnelChart), new PropertyMetadata("G6", OnChartChanged));

    private FlowChartPresenter? _presenter;
    private ChartLegendHost? _legend;

    /// <summary>Gets or sets the stage snapshot. Null clears the chart.</summary>
    public FunnelChartData? Data
    {
        get => (FunnelChartData?)this.GetValue(DataProperty);
        set => this.SetValue(DataProperty, value);
    }

    /// <summary>Gets or sets whether to display individual stages or connected ribbons.</summary>
    public FunnelChartPresentation Presentation
    {
        get => (FunnelChartPresentation)this.GetValue(PresentationProperty);
        set => this.SetValue(PresentationProperty, value);
    }

    /// <summary>Gets or sets the horizontal alignment of the bands in the Stages presentation.</summary>
    /// <remarks>Defaults to Center. Stretch is treated as Center.</remarks>
    public HorizontalAlignment StageAlignment
    {
        get => (HorizontalAlignment)this.GetValue(StageAlignmentProperty);
        set => this.SetValue(StageAlignmentProperty, value);
    }

    /// <summary>Gets or sets whether to show the cohort legend in the Flow presentation.</summary>
    public bool ShowLegend
    {
        get => (bool)this.GetValue(ShowLegendProperty);
        set => this.SetValue(ShowLegendProperty, value);
    }

    /// <summary>Gets or sets whether separate stage bands taper toward the next smaller stage. Defaults to true.</summary>
    public bool IsTapered
    {
        get => (bool)this.GetValue(IsTaperedProperty);
        set => this.SetValue(IsTaperedProperty, value);
    }

    /// <summary>Gets or sets the vertical space between separate stage bands in DIPs. Defaults to six.</summary>
    public double StageSpacing
    {
        get => (double)this.GetValue(StageSpacingProperty);
        set => this.SetValue(StageSpacingProperty, value);
    }

    /// <summary>Gets or sets the maximum corner radius in DIPs for separate stage bands. Defaults to zero.</summary>
    public double StageCornerRadius
    {
        get => (double)this.GetValue(StageCornerRadiusProperty);
        set => this.SetValue(StageCornerRadiusProperty, value);
    }

    /// <summary>Gets or sets whether stage names and values appear beside the funnel.</summary>
    public bool ShowLabels
    {
        get => (bool)this.GetValue(ShowLabelsProperty);
        set => this.SetValue(ShowLabelsProperty, value);
    }

    /// <summary>Gets or sets the numeric format for stage values and inspection.</summary>
    public string? ValueFormat
    {
        get => (string?)this.GetValue(ValueFormatProperty);
        set => this.SetValue(ValueFormatProperty, value);
    }

    /// <summary>Initializes a tapered funnel chart.</summary>
    public FunnelChart()
    {
        this.DefaultStyleKey = typeof(FunnelChart);
        FlowChartPresenter.ObserveAppearance(this, this.Render);
        ChartLegend.RegisterCallbacks(this, (_, _) => this.Render());
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        this._presenter?.Detach();
        this._presenter = null;
        this._legend = null;
        base.OnApplyTemplate();
        if (this.GetTemplateChild("PART_Plot") is Canvas plot)
        {
            this._presenter = new(
                this, plot, this.GetTemplateChild(ChartInspectorPresenter.PartName) as Popup,
                (presenter, bounds) => FunnelChartRenderer.Draw(this, presenter, bounds));
            if (this.GetTemplateChild("PART_Legend") is Grid legend)
            {
                this._legend = new ChartLegendHost(this, legend);
            }

            this.Render();
        }
    }

    private void Render()
    {
        if (this._presenter is null)
        {
            return;
        }

        var series = this.Presentation == FunnelChartPresentation.Flow && this.Data is { Stages.Count: > 0 } data ? data.Series : [];
        var showValue = ChartLegend.GetValueMode(this) == ChartLegendValueMode.Value;
        var entries = new ChartLegendEntry[series.Count];
        for (var index = 0; index < series.Count; index++)
        {
            var cohort = series[index];
            entries[index] = new(
                cohort.Label, this._presenter.Fill(index, cohort.Brush),
                showValue ? ChartInspectorPresenter.FormatValue(cohort.Values[^1], this.ValueFormat) : null);
        }

        ChartLegendSource.For(this).Update(ChartLegendSwatch.Box, entries, this._presenter.HighlightSeries);
        this._legend?.Update(this.ShowLegend);
        this._presenter.Render();
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new FlowChartAutomationPeer(this, nameof(FunnelChart), "Funnel chart");
    }

    private static void OnChartChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var chart = (FunnelChart)sender;
        if (args.Property == DataProperty || args.Property == PresentationProperty)
        {
            chart._presenter?.ClearInspection();
        }

        chart.Render();
    }
}
