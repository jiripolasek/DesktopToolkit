// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Shows weighted flows between nodes in a directed acyclic network.</summary>
[TemplatePart(Name = "PART_Plot", Type = typeof(Canvas))]
[TemplatePart(Name = ChartInspectorPresenter.PartName, Type = typeof(Popup))]
public sealed partial class SankeyChart : Control
{
    /// <summary>Identifies the <see cref="Data"/> dependency property.</summary>
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(SankeyChartData), typeof(SankeyChart), new PropertyMetadata(null, OnChartChanged));

    /// <summary>Identifies the <see cref="NodeWidth"/> dependency property.</summary>
    public static readonly DependencyProperty NodeWidthProperty = DependencyProperty.Register(
        nameof(NodeWidth), typeof(double), typeof(SankeyChart), new PropertyMetadata(16d, OnChartChanged));

    /// <summary>Identifies the <see cref="NodeSpacing"/> dependency property.</summary>
    public static readonly DependencyProperty NodeSpacingProperty = DependencyProperty.Register(
        nameof(NodeSpacing), typeof(double), typeof(SankeyChart), new PropertyMetadata(14d, OnChartChanged));

    /// <summary>Identifies the <see cref="ShowLabels"/> dependency property.</summary>
    public static readonly DependencyProperty ShowLabelsProperty = DependencyProperty.Register(
        nameof(ShowLabels), typeof(bool), typeof(SankeyChart), new PropertyMetadata(true, OnChartChanged));

    /// <summary>Identifies the <see cref="ValueFormat"/> dependency property.</summary>
    public static readonly DependencyProperty ValueFormatProperty = DependencyProperty.Register(
        nameof(ValueFormat), typeof(string), typeof(SankeyChart), new PropertyMetadata("G6", OnChartChanged));

    private FlowChartPresenter? _presenter;

    /// <summary>Gets or sets the flow snapshot. Null clears the chart.</summary>
    public SankeyChartData? Data
    {
        get => (SankeyChartData?)this.GetValue(DataProperty);
        set => this.SetValue(DataProperty, value);
    }

    /// <summary>Gets or sets the desired node width in DIPs. Defaults to sixteen.</summary>
    public double NodeWidth
    {
        get => (double)this.GetValue(NodeWidthProperty);
        set => this.SetValue(NodeWidthProperty, value);
    }

    /// <summary>Gets or sets the desired vertical space between nodes in DIPs. Defaults to fourteen.</summary>
    public double NodeSpacing
    {
        get => (double)this.GetValue(NodeSpacingProperty);
        set => this.SetValue(NodeSpacingProperty, value);
    }

    /// <summary>Gets or sets whether the chart displays node labels.</summary>
    public bool ShowLabels
    {
        get => (bool)this.GetValue(ShowLabelsProperty);
        set => this.SetValue(ShowLabelsProperty, value);
    }

    /// <summary>Gets or sets the numeric format for inspected flows and node totals.</summary>
    public string? ValueFormat
    {
        get => (string?)this.GetValue(ValueFormatProperty);
        set => this.SetValue(ValueFormatProperty, value);
    }

    /// <summary>Initializes a Sankey chart with labeled nodes.</summary>
    public SankeyChart()
    {
        this.DefaultStyleKey = typeof(SankeyChart);
        FlowChartPresenter.ObserveAppearance(this, () => this._presenter?.Render());
    }

    /// <inheritdoc />
    [WinRT.DynamicWindowsRuntimeCast(typeof(Canvas))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(Popup))]
    protected override void OnApplyTemplate()
    {
        this._presenter?.Detach();
        this._presenter = null;
        base.OnApplyTemplate();
        if (this.GetTemplateChild("PART_Plot") is Canvas plot)
        {
            this._presenter = new(
                this, plot, this.GetTemplateChild(ChartInspectorPresenter.PartName) as Popup,
                (presenter, bounds) => SankeyChartRenderer.Draw(this, presenter, bounds));
            this._presenter.Render();
        }
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new FlowChartAutomationPeer(this, nameof(SankeyChart), "SankeyChartName");
    }

    private static void OnChartChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var presenter = ((SankeyChart)sender)._presenter;
        if (args.Property == DataProperty)
        {
            presenter?.ClearInspection();
        }

        presenter?.Render();
    }
}
