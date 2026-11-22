// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Globalization;
using JPSoftworks.DesktopToolkit.Charts;
using JPSoftworks.DesktopToolkit.Localization;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
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
        VerifyLocalizedAutomationNames();
        VerifyLocalizedDateTicks();
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

    private void VerifyLocalizedAutomationNames()
    {
        var scroll = FindDescendant<ScrollViewer>(DetachedLegend, view => view.Name == "PART_ScrollViewer")
                     ?? throw new InvalidOperationException("The detached legend did not load.");
        var slices = FindDescendant<ScrollViewer>(Doughnut, view => view.Name == "PART_Legend")
                     ?? throw new InvalidOperationException("The slice legend did not load.");
        var bar = FindDescendant<Border>(Bars, item => AutomationProperties.GetName(item).StartsWith("Memory, Used:", StringComparison.Ordinal))
                  ?? throw new InvalidOperationException("The rendered bar did not have its initial description.");
        var originalLoader = ToolkitLocalization.StringResourceLoader;
        var loader = new ChartStringLoader();
        try
        {
            ToolkitLocalization.StringResourceLoader = loader;
            CheckNames("First");
            loader.Language = "Second";
            ToolkitLocalization.InvalidateStringResources();
            CheckNames("Second");

            AutomationProperties.SetName(scroll, "My legend");
            ToolkitLocalization.InvalidateStringResources();
            if (AutomationProperties.GetName(scroll) != "My legend")
            {
                throw new InvalidOperationException("A resource refresh replaced the explicit legend name.");
            }
        }
        finally
        {
            ToolkitLocalization.StringResourceLoader = originalLoader;
        }

        void CheckNames(string language)
        {
            if (AutomationProperties.GetName(scroll) != language + " legend"
                || AutomationProperties.GetName(slices) != language + " slices"
                || !AutomationProperties.GetName(bar).StartsWith(language + " Used / Memory / 42", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Existing legend and bar automation names did not follow the language change.");
            }
        }
    }

    private void VerifyLocalizedDateTicks()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousLoader = ToolkitLocalization.StringResourceLoader;
        var previousData = Line.Data;
        var start = new DateTimeOffset(2100, 1, 1, 12, 0, 0, TimeSpan.Zero);
        try
        {
            var culture = new CultureInfo("ar-SA", useUserOverride: false);
            culture.DateTimeFormat.Calendar = new UmAlQuraCalendar();
            culture.DateTimeFormat.TimeSeparator = "~";
            CultureInfo.CurrentCulture = culture;
            Line.Data = new LineChartData([new("Series", [new(start, 1), new(start.AddHours(1), 2)])]);
            var loader = new ChartStringLoader();
            ToolkitLocalization.StringResourceLoader = loader;
            CheckTick("First 2100-01-01");
            loader.Language = "Second";
            ToolkitLocalization.InvalidateStringResources();
            CheckTick("Second 2100-01-01");

            const string timeOnly = "'day' HH:mm tt";
            Line.XValueFormat = timeOnly;
            CheckTick(start.ToString(timeOnly, culture));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            Line.XValueFormat = null;
            Line.Data = previousData;
            ToolkitLocalization.StringResourceLoader = previousLoader;
        }

        void CheckTick(string expected)
        {
            if (FindDescendant<TextBlock>(Line, label => label.Text == expected) is null)
            {
                throw new InvalidOperationException("Missing visible time-axis label: " + expected);
            }
        }
    }

    private sealed class ChartStringLoader : IStringResourceLoader
    {
        public string Language { get; set; } = "First";

        public string? GetString(string catalogName, string key) => key switch
        {
            "ChartLegendName" => Language + " legend",
            "SliceLegendName" => Language + " slices",
            "MinuteTickFormat" => "'" + Language + "' yyyy-MM-dd",
            "BarDescriptionFormat" => Language + " {1} / {0} / {2}",
            _ => null,
        };
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
