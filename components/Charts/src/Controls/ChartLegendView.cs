// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Displays the legend of a chart elsewhere in the XAML layout.</summary>
[TemplatePart(Name = ScrollPartName, Type = typeof(ScrollViewer))]
public sealed partial class ChartLegendView : Control
{
    private const string ScrollPartName = "PART_ScrollViewer";

    /// <summary>Identifies the <see cref="Chart"/> dependency property.</summary>
    public static readonly DependencyProperty ChartProperty = DependencyProperty.Register(
        nameof(Chart), typeof(Control), typeof(ChartLegendView),
        new PropertyMetadata(null, static (sender, _) => ((ChartLegendView)sender).Connect()));

    private ScrollViewer? _scroll;
    private ChartLegendSource? _source;
    private ChartLegendPresenter? _presenter;

    /// <summary>Gets or sets the chart that supplies entries, values, and interaction settings.</summary>
    public Control? Chart
    {
        get => (Control?)this.GetValue(ChartProperty);
        set => this.SetValue(ChartProperty, value);
    }

    /// <summary>Creates a detached chart legend.</summary>
    public ChartLegendView()
    {
        this.DefaultStyleKey = typeof(ChartLegendView);
        this.Loaded += (_, _) => this.Connect();
        this.Unloaded += (_, _) => this.Disconnect();
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        this.Disconnect();
        base.OnApplyTemplate();
        this._scroll = this.GetTemplateChild(ScrollPartName) as ScrollViewer;
        this.Connect();
    }

    private void Connect()
    {
        this.Disconnect();
        if (!this.IsLoaded || this._scroll is null || this.Chart is not { } chart || chart == this)
        {
            return;
        }

        this._source = ChartLegendSource.For(chart);
        this._presenter = new ChartLegendPresenter(chart, this._source);
        this._scroll.Content = this._presenter.Root;
        this._source.Changed += this.Refresh;
        this.Refresh();
    }

    private void Disconnect()
    {
        if (this._source is not null)
        {
            this._source.Changed -= this.Refresh;
            this._presenter?.ClearHover();
            this._source = null;
            this._presenter = null;
        }

        if (this._scroll is not null)
        {
            this._scroll.Content = null;
        }
    }

    private void Refresh()
    {
        this._presenter?.Update(true, Orientation.Vertical);
    }
}
