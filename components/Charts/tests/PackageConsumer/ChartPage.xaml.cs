// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using JPSoftworks.DesktopToolkit.Charts;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace ChartsPackageConsumer;

public sealed partial class ChartPage : Page
{
    private static readonly Color SeriesColor = Color.FromArgb(255, 48, 128, 224);

    public LiveGraphSeriesCollection TimelineSeries { get; }
        = new(new LiveGraphSeries("Used", SeriesColor, Color.FromArgb(80, 48, 128, 224)));

    public ChartPage()
    {
        InitializeComponent();
        Bindings.Update();
        Doughnut.AddSeries("Used", SeriesColor);
        Doughnut.SetValues(42);
        Timeline.AppendSample(42);
        Bars.Data = new BarChartData(["Memory"], [new("Used", [42]), new("Free", [18])]);
        Line.Data = new LineChartData([new LineChartSeries("Memory", [new LineChartPoint(0, 42), new LineChartPoint(10, 50)])]);
    }

    internal async Task VerifyAsync()
    {
        await WaitForAsync(() =>
            IsLoaded
            && Doughnut.Template is not null
            && Timeline.Template is not null
            && Bars.Template is not null
            && Bars.ActualWidth > 0
            && Line.Template is not null);
        Line.Viewport = new LineChartViewport(2, 8);
        await WaitForAsync(() => ViewportReadout.Content is LineChartViewport { Minimum: 2, Maximum: 8 });
        Line.ResetViewport();
        await WaitForAsync(() => ViewportReadout.Content is null);
        await WaitForAsync(() => FindDescendant<ContentPresenter>(
            Bars,
            element => element.Content is ChartLegendItem { Name: "Used" }
                       && FindDescendant<TextBlock>(element, label => label.Text == "Used") is not null) is not null);
        var doughnut = FindDescendant<CanvasControl>(Doughnut)
                       ?? throw new InvalidOperationException("The doughnut canvas was not created.");
        var timeline = FindDescendant<CanvasAnimatedControl>(Timeline)
                       ?? throw new InvalidOperationException("The live graph canvas was not created.");

        var doughnutDrew = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var timelineDrew = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnDoughnutDraw(CanvasControl sender, CanvasDrawEventArgs args) => doughnutDrew.TrySetResult();
        void OnTimelineDraw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args) => timelineDrew.TrySetResult();

        doughnut.Draw += OnDoughnutDraw;
        timeline.Draw += OnTimelineDraw;
        try
        {
            doughnut.Invalidate();
            await Task.WhenAll(doughnutDrew.Task, timelineDrew.Task).WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            doughnut.Draw -= OnDoughnutDraw;
            timeline.Draw -= OnTimelineDraw;
        }
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(50);
        }

        throw new InvalidOperationException("The chart templates did not load.");
    }

    private static T? FindDescendant<T>(DependencyObject element, Func<T, bool>? matches = null)
        where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(element); index++)
        {
            var child = VisualTreeHelper.GetChild(element, index);
            if (child is T result && (matches is null || matches(result)))
            {
                return result;
            }

            if (FindDescendant(child, matches) is { } descendant)
            {
                return descendant;
            }
        }

        return null;
    }
}
