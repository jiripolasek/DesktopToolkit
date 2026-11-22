// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Globalization;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using JPSoftworks.DesktopToolkit.Charts;
using JPSoftworks.DesktopToolkit.Localization;
using RangePicker = JPSoftworks.DesktopToolkit.Controls.CalendarDateRangePicker;
using JPSoftworks.DesktopToolkit.Controls.Demo.Pages;
using JPSoftworks.DesktopToolkit.Controls.Demo.Controls;
using JPSoftworks.DesktopToolkit.Controls.Demo.Services;
using JPSoftworks.DesktopToolkit.Gallery.UI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI;
using ChartPath = Microsoft.UI.Xaml.Shapes.Path;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Diagnostics;

// Linked only when EnableSmokeTests=true. No test types are shipped in the library
// or compiled into the ordinary demo.
internal static class PackageSmokeTests
{
    private static readonly List<string> Checks = [];

    internal static async Task RunAsync(MainWindow window, string resultPath)
    {
        try
        {
            await Task.Delay(300);
            await VerifyContractsAsync();
            VerifyMalformedChartFormats();
            VerifyFlowChartAutomationNames();
            VerifyReferenceLineOptions();
            VerifyMilestoneContracts();
            VerifyLinkedViewports();
            var root = (FrameworkElement)window.Content;
            var frame = Descendants(root).OfType<Frame>().Single();
            var titleBar = Descendants(root).OfType<TitleBar>().Single();
            Assert(
                titleBar.IsPaneToggleButtonVisible && titleBar.Content is AutoSuggestBox,
                "Custom title bar contains pane toggle and search");
            var navigation = Descendants(root).OfType<NavigationView>().Single();
            Assert(
                navigation.DisplayMode != NavigationViewDisplayMode.Expanded || navigation.IsPaneOpen,
                "Expanded navigation starts with its pane open");
            Assert(!frame.CanGoBack && !titleBar.IsBackButtonVisible, "The initial overview has no back navigation");
            await VerifyInitiallyPausedGraphAsync(root);
            await VerifyCompactSparklineAsync(root);
            VerifyFilledPieRenderer();
            await VerifyResponsiveDoughnutLegendAsync(root);
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                root.RequestedTheme = theme;
                foreach (var tag in new[]
                         {
                             "overview", "live", "line-chart", "scatter-chart", "spider-chart", "heatmap", "sankey", "funnel",
                             "treemap", "icicle", "sunburst", "bar-chart", "bar", "gauge", "doughnut", "legends", "dashboard-widgets",
                             "monitor", "memory-analysis", "sales-performance", "web-analytics", "settings", "live", "doughnut"
                         })
                {
                    window.NavigateTo(tag);
                    await Task.Delay(200);
                    await WaitForAsync(() => frame.Content is Page { IsLoaded: true });
                    Assert(frame.Content is Page { IsLoaded: true }, $"{theme}: {tag} page loaded");
                    var controls = GetSampleControls((DependencyObject)frame.Content);
                    if (theme == ElementTheme.Light)
                    {
                        VerifyStylingGuide((Page)frame.Content);
                    }

                    if (tag is "live" or "line-chart" or "scatter-chart" or "spider-chart" or "heatmap" or "treemap" or "icicle"
                        or "sunburst" or "bar-chart" or "bar" or "gauge" or "doughnut" or "sankey" or "funnel")
                    {
                        VerifySampleCustomization(controls.OfType<ChartSampleOptions>().First());
                    }

                    foreach (var control in controls.Where(item => item is BarChart or LineChart or ScatterChart or TreemapChart
                                 or IcicleChart or SunburstChart or LiveAreaGraph or VerticalUsageBar or RadialGauge or DoughnutGraph
                                 or SpiderChart or HeatmapChart or SankeyChart or FunnelChart))
                    {
                        if (tag == "web-analytics" && control is (BarChart or HeatmapChart or FunnelChart))
                        {
                            continue;
                        }

                        Assert(
                            control.Template != null && control.ActualWidth > 0 && control.ActualHeight > 0,
                            $"{theme}: {tag} {control.GetType().Name} template and layout");
                        if (control is VerticalUsageBar)
                        {
                            Assert(Descendants(control).OfType<Canvas>().Any(), $"{theme}: {tag} meter visuals");
                        }
                        else if (control is LiveAreaGraph)
                        {
                            Assert(Descendants(control).OfType<CanvasAnimatedControl>().Any(), $"{theme}: {tag} animated canvas created");
                        }
                        else if (control is DoughnutGraph)
                        {
                            Assert(Descendants(control).OfType<CanvasControl>().Any(), $"{theme}: {tag} canvas created");
                        }
                    }

                    if (tag == "bar-chart")
                    {
                        VerifyBarChartGallery(controls);
                    }
                    else if (tag == "line-chart")
                    {
                        VerifySharedSampleHeader((Page)frame.Content);
                        await VerifyLineChartGalleryAsync(controls);
                        await VerifyLineLegendAsync(controls);
                    }
                    else if (tag == "scatter-chart")
                    {
                        await VerifyScatterChartGalleryAsync(controls);
                    }
                    else if (tag == "spider-chart")
                    {
                        await VerifySpiderGalleryAsync(controls);
                    }
                    else if (tag == "heatmap")
                    {
                        await VerifyHeatmapGalleryAsync(controls);
                    }
                    else if (tag == "sankey")
                    {
                        VerifySankeyGallery(controls.OfType<SankeyChart>().Single());
                    }
                    else if (tag == "funnel")
                    {
                        VerifyFunnelGallery(controls);
                    }
                    else if (tag == "treemap")
                    {
                        await VerifyTreemapGalleryAsync(controls);
                    }
                    else if (tag == "icicle")
                    {
                        await VerifyIcicleGalleryAsync(controls);
                    }
                    else if (tag == "sunburst")
                    {
                        await VerifySunburstGalleryAsync(controls);
                    }
                    else if (tag == "monitor")
                    {
                        await VerifySystemMonitorAsync(controls);
                    }
                    else if (tag == "memory-analysis")
                    {
                        VerifyMemoryAnalysis(controls);
                    }
                    else if (tag == "dashboard-widgets")
                    {
                        VerifyDashboardWidgets(controls);
                    }
                    else if (tag == "sales-performance")
                    {
                        VerifySalesPerformance(controls);
                    }
                    else if (tag == "web-analytics")
                    {
                        await VerifyWebAnalyticsAsync((Page)frame.Content);
                        var reportScroll = (ScrollViewer)((Page)frame.Content).FindName("ReportScroll");
                        var reportBottom = reportScroll.TransformToVisual(root).TransformPoint(new Point(0, reportScroll.ActualHeight)).Y;
                        Assert(
                            frame.Margin.Bottom == 0 && Math.Abs(reportBottom - root.ActualHeight) <= 1,
                            "The Web analytics scrolling viewport reaches the bottom of the gallery");
                    }
                    else if (tag == "bar")
                    {
                        VerifyHorizontalMeterLayout(controls.OfType<VerticalUsageBar>().Single(bar => bar.Name == "HorizontalStackedBar"));
                        await VerifyCompactSegmentedMeterAsync(controls);
                    }
                    else if (tag == "gauge")
                    {
                        await VerifyRadialGaugeGalleryAsync(controls);
                    }
                    else if (tag == "live")
                    {
                        await VerifyLiveGraphLegendsAsync((Page)frame.Content);
                        var sparkline = controls.OfType<LiveAreaGraph>().Single(graph => graph.IsSparkline);
                        Assert(
                            Near(sparkline.ActualHeight, 56)
                            && sparkline.Series.Count == 1
                            && sparkline.ShowAreaFill
                            && sparkline.ShowLatestValueMarker,
                            $"{theme}: the dashboard tile displays a compact CPU sparkline with fill and marker");
                    }
                    else if (tag == "doughnut")
                    {
                        var charts = controls.OfType<DoughnutGraph>().Where(chart => chart.ShowLegend).ToArray();
                        Assert(
                            charts.Length == 2
                            && charts.Any(chart => chart.Mode == DoughnutGraphMode.Doughnut)
                            && charts.Any(chart => chart.Mode == DoughnutGraphMode.Pie),
                            $"{theme}: the gallery shows both slice presentations with legends");
                        Assert(
                            charts.All(chart =>
                                GetLegendShares(GetDoughnutLegend(chart)).SequenceEqual(new[] { Share(0.42), Share(0.26), Share(0.32) })),
                            $"{theme}: the gallery legends report the same storage shares");
                    }
                    else if (tag == "legends")
                    {
                        await VerifyLegendPlacementAsync(controls);
                    }

                    foreach (var canvas in Descendants((DependencyObject)frame.Content).OfType<CanvasControl>())
                    {
                        var draws = 0;
                        void OnDraw(CanvasControl sender, CanvasDrawEventArgs args) => Interlocked.Increment(ref draws);
                        canvas.Draw += OnDraw;
                        try
                        {
                            canvas.Invalidate();
                            await WaitForAsync(() => Volatile.Read(ref draws) > 0);
                            Assert(true, $"{theme}: {tag} Win2D draw");
                        }
                        finally
                        {
                            canvas.Draw -= OnDraw;
                        }
                    }

                    foreach (var canvas in Descendants((DependencyObject)frame.Content).OfType<CanvasAnimatedControl>())
                    {
                        var draws = 0;
                        void OnDraw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args) => Interlocked.Increment(ref draws);
                        canvas.Draw += OnDraw;
                        try
                        {
                            canvas.Invalidate();
                            await WaitForAsync(() => Volatile.Read(ref draws) > 0);
                            Assert(true, $"{theme}: {tag} animated Win2D draw");
                        }
                        finally
                        {
                            canvas.Draw -= OnDraw;
                        }
                    }
                }
            }

            window.AppWindow.Resize(new Windows.Graphics.SizeInt32(720, 900));
            window.NavigateTo("line-chart");
            await Task.Delay(400);
            await VerifyLineChartGalleryAsync(GetSampleControls((DependencyObject)frame.Content));
            window.NavigateTo("bar-chart");
            await Task.Delay(400);
            VerifyBarChartGallery(GetSampleControls((DependencyObject)frame.Content));
            window.NavigateTo("bar");
            await Task.Delay(400);
            foreach (var example in Descendants((DependencyObject)frame.Content).OfType<ExampleCard>())
            {
                var sidebar = (Border)example.FindName("SampleSidebar");
                var options = (Button)example.FindName("OptionsButton");
                Assert(
                    sidebar.Visibility == Visibility.Visible
                    && Grid.GetColumn(sidebar) == 0
                    && Grid.GetRow(sidebar) == 1
                    && sidebar.ActualWidth <= example.ActualWidth,
                    $"{example.Header}: sample controls stack below the chart in a narrow window");
                Assert(
                    options.Visibility == (example.Options is null ? Visibility.Collapsed : Visibility.Visible) && options.Flyout is Flyout,
                    $"{example.Header}: the sidebar offers the options flyout when the example has options");
                example.ShowSource();
                Assert(((Expander)example.FindName("SourceExpander")).IsExpanded, $"{example.Header}: sample source expands");
            }

            File.WriteAllText(
                resultPath, JsonSerializer.Serialize(new { Passed = true, Checks }, new JsonSerializerOptions { WriteIndented = true }));
            Application.Current.Exit();
        }
        catch (Exception error)
        {
            File.WriteAllText(
                resultPath,
                JsonSerializer.Serialize(
                    new { Passed = false, Checks, Error = error.ToString() }, new JsonSerializerOptions { WriteIndented = true }));
            Environment.ExitCode = 1;
            Application.Current.Exit();
        }
    }

    private static void VerifySharedSampleHeader(Page page)
    {
        var header = Descendants(page).OfType<GalleryPageHeader>().Single();
        var info = (Button)header.FindName("ApiInformationButton");
        var toggle = (ToggleButton)header.FindName("PreviewThemeToggle");
        var previews = Descendants(page).OfType<ExampleCard>().Select(card => (Border)card.FindName("PreviewBorder")).ToArray();

        Assert(
            info.Flyout is Flyout
            && header.ApiNamespace == "JPSoftworks.DesktopToolkit.Charts"
            && !string.IsNullOrWhiteSpace(header.ApiNotes), "The shared header shows the chart API information");
        Assert(
            previews.Length > 1 && toggle.IsChecked == false && previews.All(preview => preview.RequestedTheme == ElementTheme.Default),
            "Sample previews follow the gallery theme by default");

        var galleryTheme = header.ActualTheme;
        var alternate = galleryTheme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;
        toggle.IsChecked = true;
        Assert(
            previews.All(preview => preview.RequestedTheme == alternate) && header.ActualTheme == galleryTheme,
            "The shared theme toggle switches every sample preview and leaves the page header theme alone");
        toggle.IsChecked = false;
        Assert(
            previews.All(preview => preview.RequestedTheme == ElementTheme.Default),
            "Turning the shared theme toggle off restores inherited themes");
    }

    private static void VerifyStylingGuide(Page page)
    {
        var header = Descendants(page).OfType<SamplePageHeader>().SingleOrDefault();
        if (header?.StylingControl is not { } control)
        {
            return;
        }

        var guide = ChartStylingGuide.Get(control);
        Assert(
            guide is { Entries.Length: 4, Xaml.Length: > 0 } && header.StylingExample is { } name && page.FindName(name) is ExampleCard,
            $"{control} links to its styling guide and a live preview");

        var dictionary = (ResourceDictionary)Microsoft.UI.Xaml.Markup.XamlReader.Load(
            $$"""
              <ResourceDictionary
                  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                  xmlns:charts="using:JPSoftworks.DesktopToolkit.Charts">
                  {{guide!.Xaml}}
              </ResourceDictionary>
              """);
        Assert(
            dictionary.Values.OfType<Style>().Single().TargetType.Name == control,
            $"{control} styling example loads as a keyed XAML style");
    }

    private static void VerifySampleCustomization(ChartSampleOptions options)
    {
        var chart = options.Target ?? throw new InvalidOperationException("The sample options need a chart target.");
        var inspector = (ToggleSwitch)options.FindName("InspectorSwitch");
        var delay = (Slider)options.FindName("HoverDelaySlider");
        var motion = (Slider)options.FindName("SlideDurationSlider");
        var originalDelay = delay.Value;
        var originalMotion = motion.Value;
        var originalInspection = inspector.IsOn;
        Assert(
            TimeSpan.FromMilliseconds(originalDelay) == ChartInspection.GetHoverDelay(chart)
            && TimeSpan.FromMilliseconds(originalMotion) == ChartInspection.GetTransitionDuration(chart)
            && originalInspection == ChartInspection.GetIsEnabled(chart),
            "Gallery options start at the chart's effective inspection settings");
        delay.Value = 350;
        motion.Value = 0;
        inspector.IsOn = false;
        Assert(
            !ChartInspection.GetIsEnabled(chart)
            && ChartInspection.GetHoverDelay(chart) == TimeSpan.FromMilliseconds(350)
            && ChartInspection.GetTransitionDuration(chart) == TimeSpan.Zero,
            "The shared gallery inspector settings write through to the chart");
        inspector.IsOn = originalInspection;
        delay.Value = originalDelay;
        motion.Value = originalMotion;

        var axes = (ComboBox)options.FindName("AxesBox");
        if (axes.Visibility == Visibility.Visible)
        {
            var plot = Descendants(chart).OfType<Canvas>().Single(item => item.Name == "PART_Plot");
            var originalIndex = axes.SelectedIndex;
            Assert(plot.Children.OfType<TextBlock>().Any(), "The standard preview displays axis labels");
            if (chart is BarChart or LineChart or ScatterChart)
            {
                axes.SelectedIndex = 1;
                Assert(
                    plot.Children.OfType<TextBlock>().Any()
                    && !plot.Children.OfType<Line>().Any(line => line.Visibility == Visibility.Visible && line.Clip is null),
                    "The labels-only preset removes Cartesian grid and axis lines");
            }

            axes.SelectedIndex = 2;
            ChartAxisOptions?[] axisOptions = chart switch
            {
                BarChart bar => [bar.ValueAxis],
                LineChart line => [line.XAxis, line.YAxis],
                ScatterChart scatter => [scatter.XAxis, scatter.YAxis],
                _ => [],
            };
            var referenceLabels = axisOptions.SelectMany(axis => axis?.ReferenceLines ?? Array.Empty<ChartReferenceLine>())
                .Select(reference => reference.Label).ToHashSet();
            Assert(
                ChartAxisAppearance.GetPresentation(chart) == ChartAxisPresentation.Hidden
                && plot.Children.OfType<TextBlock>().All(label => referenceLabels.Contains(label.Text)),
                "The common axes picker hides tick labels while allowing reference captions");
            axes.SelectedIndex = originalIndex;
            Assert(plot.Children.OfType<TextBlock>().Any(), "Restoring the axis preset redraws the labels");
        }
    }

    private static void VerifyMalformedChartFormats()
    {
        var previous = ToolkitLocalization.StringResourceLoader;
        var loader = new ChartStringLoader();
        var gauge = new RadialGauge { Maximum = 100, BandsSource = [new(0, 100, default, "Normal")] };
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(gauge)!;
        try
        {
            ToolkitLocalization.StringResourceLoader = loader;
            foreach (var pattern in new[] { "{99}", "{" })
            {
                loader.Strings["ListPairFormat"] = pattern;
                ToolkitLocalization.InvalidateStringResources();
                gauge.Value = 1;
                var exceptions = CountFormattingExceptions(() =>
                {
                    for (var value = 2; value <= 100; value++)
                    {
                        gauge.Value = value;
                    }
                });

                Assert(exceptions == 0 && peer.GetItemStatus() == "100, Normal",
                    $"Repeated gauge updates skip the invalid format {pattern} and display the fallback status");
            }

            loader.Strings["ListPairFormat"] = "{{literal}}";
            ToolkitLocalization.InvalidateStringResources();
            Assert(peer.GetItemStatus() == "{literal}", "A stationary gauge refreshes its accessible status using the replacement literal format");
        }
        finally
        {
            ToolkitLocalization.StringResourceLoader = previous;
        }
    }

    private static void VerifyMalformedDateTickFormats(LineChart chart, DateTimeOffset start)
    {
        var previousLoader = ToolkitLocalization.StringResourceLoader;
        var previousCulture = CultureInfo.CurrentCulture;
        var previousFormat = chart.XValueFormat;
        var previousGrid = chart.ShowGrid;
        var loader = new ChartStringLoader();
        try
        {
            loader.Strings["MinuteTickFormat"] = "HH:mm '";
            ToolkitLocalization.StringResourceLoader = loader;
            chart.XValueFormat = "Q";
            var exceptions = CountFormattingExceptions(() =>
            {
                for (var redraw = 0; redraw < 4; redraw++)
                {
                    chart.ShowGrid = !chart.ShowGrid;
                }
            });
            Assert(HasTickLabels("HH:mm") && exceptions == 0,
                $"Redrawing with invalid override and resource date patterns uses the fallback ticks ({exceptions} format exceptions)");

            const string translation = "HH:mm:ss 'UTC'";
            loader.Strings["MinuteTickFormat"] = translation;
            ToolkitLocalization.InvalidateStringResources();
            Assert(HasTickLabels(translation), "A stationary chart refreshes its visible tick labels on invalidation");

            const string custom = "yyyy-MM-dd HH:mm";
            chart.XValueFormat = custom;
            Assert(HasTickLabels(custom), "A valid date override takes precedence over the translated tick format");

            var culture = (CultureInfo)previousCulture.Clone();
            culture.DateTimeFormat.TimeSeparator = "~";
            CultureInfo.CurrentCulture = culture;
            chart.ShowGrid = !chart.ShowGrid;
            Assert(HasTickLabels(custom), "A cached date pattern uses the current culture for each tick");
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            ToolkitLocalization.StringResourceLoader = previousLoader;
            chart.XValueFormat = previousFormat;
            chart.ShowGrid = previousGrid;
        }

        bool HasTickLabels(string pattern)
        {
            var labels = GetLinePlot(chart).Children.OfType<TextBlock>().Select(label => label.Text).ToArray();
            return labels.Contains(start.ToString(pattern, CultureInfo.CurrentCulture))
                   && labels.Contains(start.AddHours(1).ToString(pattern, CultureInfo.CurrentCulture));
        }
    }

    private static void VerifyCalendarRangeTickFormats(LineChart chart)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousData = chart.Data;
        var previousFormat = chart.XValueFormat;
        var previousGrid = chart.ShowGrid;
        try
        {
            var culture = new CultureInfo("ar-SA", useUserOverride: false);
            culture.DateTimeFormat.Calendar = new UmAlQuraCalendar();
            CultureInfo.CurrentCulture = culture;
            const string format = "yyyy-MM-dd HH:mm";
            chart.XValueFormat = format;
            foreach (var year in new[] { 1890, 2100, 2026 })
            {
                var start = new DateTimeOffset(year, 1, 1, 12, 0, 0, TimeSpan.Zero);
                var end = start.AddHours(1);
                var exceptions = CountFormattingExceptions(() =>
                {
                    chart.Data = new LineChartData([new("Series", [new(start, 1), new(end, 2)])]);
                    chart.ShowGrid = !chart.ShowGrid;
                });
                var expectedCulture = year == 2026 ? culture : CultureInfo.InvariantCulture;
                var labels = GetLinePlot(chart).Children.OfType<TextBlock>().Select(label => label.Text).ToArray();
                Assert(exceptions == 0 && labels.Contains(start.ToString(format, expectedCulture)) && labels.Contains(end.ToString(format, expectedCulture)),
                    $"Year {year} uses the applicable calendar across redraws ({exceptions} formatting exceptions)");
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            chart.Data = previousData;
            chart.XValueFormat = previousFormat;
            chart.ShowGrid = previousGrid;
        }
    }

    private static int CountFormattingExceptions(Action action)
    {
        var thread = Environment.CurrentManagedThreadId;
        var exceptions = 0;
        AppDomain.CurrentDomain.FirstChanceException += OnException;
        try
        {
            action();
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= OnException;
        }

        return exceptions;

        void OnException(object? sender, FirstChanceExceptionEventArgs args)
        {
            if (Environment.CurrentManagedThreadId == thread && args.Exception is FormatException or ArgumentOutOfRangeException)
            {
                exceptions++;
            }
        }
    }

    private static void VerifyFlowChartAutomationNames()
    {
        var previous = ToolkitLocalization.StringResourceLoader;
        var loader = new ChartStringLoader();
        try
        {
            ToolkitLocalization.StringResourceLoader = loader;
            var funnel = new FunnelChart();
            var funnelPeer = FrameworkElementAutomationPeer.CreatePeerForElement(funnel)!;
            var sankeyPeer = FrameworkElementAutomationPeer.CreatePeerForElement(new SankeyChart())!;
            Assert(funnelPeer.GetName() == "Funnel chart" && sankeyPeer.GetName() == "Sankey chart",
                "Flow-chart automation peers expose their default names");

            loader.Strings["FunnelChartName"] = "Trichterdiagramm";
            loader.Strings["SankeyChartName"] = "Sankey-Diagramm";
            ToolkitLocalization.InvalidateStringResources();
            Assert(funnelPeer.GetName() == "Trichterdiagramm" && sankeyPeer.GetName() == "Sankey-Diagramm",
                "Existing funnel and Sankey automation peers resolve the new translations");

            AutomationProperties.SetName(funnel, "Checkout stages");
            loader.Strings.Clear();
            ToolkitLocalization.InvalidateStringResources();
            Assert(funnelPeer.GetName() == "Checkout stages" && sankeyPeer.GetName() == "Sankey chart",
                "An explicit flow-chart name is preserved when the built-in names are restored");
        }
        finally
        {
            ToolkitLocalization.StringResourceLoader = previous;
        }
    }

    private sealed class ChartStringLoader : IStringResourceLoader
    {
        public Dictionary<string, string> Strings { get; } = [];

        public string? GetString(string catalogName, string key) => this.Strings.GetValueOrDefault(key);
    }

    private static async Task VerifyContractsAsync()
    {
        VerifyLineChartDataContracts();
        VerifyViewportContracts();
        VerifyScatterChartDataContracts();
        await VerifyBindableMetersAsync();
        VerifyMatrixAndSpiderData();
        VerifyChartPalettes();
        VerifyNetworkCounters();
        VerifyHierarchyChartData();
        VerifyTreemapLayouts();
        VerifyRadialGaugeBandContracts();
        VerifyLiveSmoothing();
        VerifyLiveHistoryBoundaries();
        var blue = Color.FromArgb(255, 0, 120, 212);
        var values = new double?[] { 12, -6, null };
        var series = new BarChartSeries("Net change", values);
        var labels = new[] { "A", "B", "C" };
        var data = new BarChartData(labels, [series]);
        values[0] = 99;
        labels[0] = "Other";
        Assert(
            data.Categories[0] == "A" && data.Series[0].Values[0] == 12 && data.Series[0].Values[2] is null,
            "BarChartData snapshots the source arrays and preserves missing values");
        Throws<ArgumentException>(
            () => new BarChartSeries("Invalid", [double.PositiveInfinity]), "BarChartSeries rejects non-finite values");
        Throws<ArgumentException>(() => new BarChartData(["A"], [series]), "BarChartData requires one value per category per series");

        var graph = new LiveAreaGraph();
        Throws<InvalidOperationException>(() => graph.AppendSample(1), "A graph requires series");
        graph.AddSeries("CPU", blue, blue);
        Throws<ArgumentException>(() => graph.AddSeries("CPU", blue, blue), "Graph series names are unique");
        Throws<ArgumentException>(() => graph.AppendSample(double.NaN), "Non-finite graph values are rejected");
        Throws<ArgumentException>(() => graph.AppendSample(1, 2), "Graph value counts must match series");
        var now = DateTimeOffset.UtcNow;
        graph.AppendSample(now, 42);
        Throws<ArgumentOutOfRangeException>(() => graph.AppendSample(now.AddSeconds(-1), 50), "Out-of-order timestamps are rejected");
        graph.IsPaused = true;
        Throws<InvalidOperationException>(() => graph.AppendSample(42), "Appending while paused is rejected");
        graph.IsPaused = false;
        await Task.Run(() => graph.AppendSample(55));
        graph.Clear();
        Assert(graph.Series.Count == 1, "Clearing graph history preserves series");

        var bar = new VerticalUsageBar { Minimum = 0, Maximum = 100, Value = 37 };
        bar.AddSeries("Used", new SolidColorBrush(blue));
        bar.AddSeries("Free", new SolidColorBrush(blue));
        bar.SetValues(42, 58);
        Assert(bar.Value == 37, "Stacked contributions preserve the accessible headline value");
        Throws<ArgumentOutOfRangeException>(() => bar.SetValues(-1, 2), "Negative meter contributions are rejected");
        Throws<ArgumentException>(() => bar.SetValues(1), "Meter value counts must match series");
        Throws<InvalidOperationException>(() => bar.AddSeries("Late", new SolidColorBrush(blue)), "Meter series freeze after values");
        await Task.Run(() => bar.SetValues(80, 20));
        await Task.Delay(100);
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(bar);
        var range = peer?.GetPattern(PatternInterface.RangeValue) as IRangeValueProvider;
        Assert(range is { IsReadOnly: true, Value: 37 }, "Meter exposes read-only range automation");

        var doughnut = new DoughnutGraph();
        doughnut.AddSeries("Used", blue);
        doughnut.AddSeries("Free", blue);
        Throws<ArgumentOutOfRangeException>(() => doughnut.SetValues(1, double.PositiveInfinity), "Infinite slice values are rejected");
        Throws<ArgumentOutOfRangeException>(() => doughnut.SetValues(-1, 1), "Negative slices are rejected");
        Throws<ArgumentException>(() => doughnut.SetValues(1), "Slice counts must match series");
        doughnut.SetValues(double.MaxValue, double.MaxValue);
        doughnut.SetValues(0, 0);
        await Task.Run(() => doughnut.SetValues(40, 60));
        await Task.Delay(100);
        doughnut.Clear();
        Assert(doughnut.Series.Count == 2, "Large, zero, producer-thread and clear updates preserve slices");
    }

    private static void VerifyLineChartDataContracts()
    {
        var points = new LineChartPoint[] { new(-5, 5), new(10, null), new(10, -2) };
        var series = new LineChartSeries("Numeric", points);
        var seriesSource = new[] { series };
        var data = new LineChartData(seriesSource);
        points[0] = new(99, 99);
        seriesSource[0] = new LineChartSeries("Replacement", []);
        Assert(
            !data.IsTimeBased
            && data.Series[0].Name == "Numeric"
            && series.Points[0].X == -5
            && series.Points[1].Y is null
            && series.Points[2].Y == -2, "LineChartData snapshots ordered numeric points, including equal X values and gaps");
        Throws<ArgumentOutOfRangeException>(() => new LineChartPoint(double.NaN, 0), "LineChartPoint rejects non-finite X");
        Throws<ArgumentOutOfRangeException>(() => new LineChartPoint(0, double.PositiveInfinity), "LineChartPoint rejects non-finite Y");
        Throws<ArgumentException>(
            () => new LineChartSeries("Unordered", [new(2, 2), new(1, 1)]), "LineChartSeries rejects descending coordinates");
        var start = new DateTimeOffset(2026, 1, 12, 9, 0, 0, TimeSpan.Zero);
        var timed = new LineChartSeries("Timed", [new(start.ToOffset(TimeSpan.FromHours(2)), 1), new(start.AddMinutes(2), 2)]);
        Assert(new LineChartData([timed]).IsTimeBased, "LineChartSeries orders timestamps by instant across offsets");
        Throws<ArgumentException>(
            () => new LineChartSeries("Mixed", [new(0, 0), new(start, 0)]), "LineChartSeries rejects mixed axis types");
        Throws<ArgumentException>(() => new LineChartData([series, timed]), "LineChartData rejects mixed axis types across series");
    }

    private static void VerifyScatterChartDataContracts()
    {
        var points = new ScatterChartPoint[] { new(8, 24), new(2, 12), new(8, 31, "Retry") };
        var series = new ScatterChartSeries("Latency", points);
        var source = new[] { series };
        var data = new ScatterChartData(source);
        source[0] = new ScatterChartSeries("Replacement", []);
        points[0] = new(1, 1);
        Assert(
            data.Series[0] == series
            && series.Points[0] == new ScatterChartPoint(8, 24)
            && series.Points[2] == new ScatterChartPoint(8, 31, "Retry"),
            "Scatter data snapshots unsorted points with duplicate X coordinates and optional labels");
        Throws<ArgumentOutOfRangeException>(() => new ScatterChartPoint(double.NaN, 0), "Scatter X coordinates must be finite");
        Throws<ArgumentOutOfRangeException>(
            () => new ScatterChartPoint(0, double.PositiveInfinity), "Scatter Y coordinates must be finite");
    }

    private static void VerifyNetworkCounters()
    {
        var previous = new NetworkCounterSample(
            0,
            new Dictionary<string, AdapterCounters>
            {
                ["active"] = new(10000, 2000), ["reset"] = new(5000, 3000), ["removed"] = new(3000, 1500),
            });
        var current = new NetworkCounterSample(
            System.Diagnostics.Stopwatch.Frequency * 2,
            new Dictionary<string, AdapterCounters>
            {
                ["active"] = new(14096, 4048), ["reset"] = new(1000, 1000), ["new"] = new(7000, 5000),
            });
        Assert(
            current.GetRatesSince(previous) == new NetworkRates(2, 1),
            "Network rates use elapsed time and skip reset, new, and removed adapters");
        Assert(
            current.GetRatesSince(null) is null && previous.GetRatesSince(current) is null,
            "Network throughput requires a baseline with an earlier timestamp");
    }

    private static async Task VerifySystemMonitorAsync(Control[] controls)
    {
        var graph = controls.OfType<LiveAreaGraph>().Single(item => item.Name == "History");
        var section = controls.OfType<Expander>().Single(item => item.Name == "NetworkSection");
        var network = ((Panel)section.Content).Children.OfType<LiveAreaGraph>().Single();
        var pause = controls.OfType<Button>().Single(item => item.Name == "PauseButton");
        var window = controls.OfType<ComboBox>().Single(item => item.Name == "HistoryBox");
        Assert(!graph.IsPaused && network.IsPaused && !section.IsExpanded, "The system monitor only samples network on demand");
        section.IsExpanded = true;
        await WaitForAsync(() => !network.IsPaused);
        Assert(
            network.Series.Select(item => item.Name).SequenceEqual((string[])["Receive", "Send"]),
            "Expanding the network panel enables both transfer histories");
        window.SelectedIndex = 2;
        await WaitForAsync(() => graph.HistoryDuration == TimeSpan.FromMinutes(2) && network.HistoryDuration == graph.HistoryDuration);
        var action = (IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(pause)!.GetPattern(PatternInterface.Invoke)!;
        action.Invoke();
        await WaitForAsync(() => graph.IsPaused && network.IsPaused);
        Assert((string)pause.Content == "Resume", "Pausing the monitor freezes both graphs together");
        action.Invoke();
        await WaitForAsync(() => !graph.IsPaused && !network.IsPaused);
        section.IsExpanded = false;
        Assert(network.IsPaused && !graph.IsPaused, "Collapsing network leaves the CPU and memory chart running");
    }

    private static void VerifyViewportContracts()
    {
        var chart = new LineChart { Data = new LineChartData([new("Numeric", [new(0, 1), new(100, 2)])]), };
        Assert(
            chart.Data.Extent == new LineChartViewport(0, 100)
            && chart.ZoomBy(2, 0.25)
            && chart.VisibleViewport == new LineChartViewport(12.5, 62.5), "Zoom holds the chosen point within the visible numeric range");
        Assert(
            chart.PanBy(1) && chart.VisibleViewport == new LineChartViewport(50, 100) && !chart.PanBy(1),
            "Panning clamps at the final sample");
        Assert(chart.ZoomBy(0.5) && chart.VisibleViewport is null, "Zooming out fits the full range");
        Throws<ArgumentException>(() => new LineChartViewport(2, 2), "A numeric viewport must have a positive span");
        Throws<ArgumentOutOfRangeException>(() => chart.ZoomBy(double.NaN), "Zoom rejects an invalid factor");

        var start = new DateTimeOffset(2026, 1, 1, 10, 30, 0, TimeSpan.FromHours(2));
        var end = start.AddMinutes(2);
        chart.Data = new LineChartData([new("Time", [new(start, 1), new(end, 2)])]);
        chart.Viewport = new LineChartViewport(0, 40);
        Assert(
            chart.Data.Extent?.Start == start.ToUniversalTime()
            && chart.VisibleViewport is null
            && chart.ZoomBy(2, 1)
            && chart.VisibleViewport == new LineChartViewport(start.AddMinutes(1), end),
            "Time navigation uses the UTC data extent and ignores numeric viewport requests");
    }

    private static void VerifyLinkedViewports()
    {
        var group = new LineChartViewportGroup();
        var first = new LineChart { ViewportGroup = group, Data = CreateData(0, 60) };
        var second = new LineChart { ViewportGroup = group, Data = CreateData(40, 100) };
        Assert(
            first.VisibleViewport == new LineChartViewport(0, 100) && second.VisibleViewport == first.VisibleViewport,
            "Linked numeric charts fit their combined extent");
        Assert(
            second.ZoomBy(2, 0)
            && first.VisibleViewport == new LineChartViewport(0, 50)
            && second.VisibleViewport == first.VisibleViewport
            && first.Viewport is null
            && second.Viewport is null,
            "Zooming from either member shares absolute X bounds without changing the requested viewport properties");
        second.Data = CreateData(50, 120);
        Assert(
            first.VisibleViewport == new LineChartViewport(0, 50) && second.VisibleViewport == first.VisibleViewport,
            "Replacing a member's data preserves the shared window");
        first.ResetViewport();
        Assert(
            first.VisibleViewport == new LineChartViewport(0, 120) && second.VisibleViewport == first.VisibleViewport,
            "Fitting a linked chart includes the updated sample range");
        var timeChart = new LineChart { ViewportGroup = group };
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        timeChart.Data = new LineChartData([new("Time", [new(start, 1), new(start.AddMinutes(2), 3)])]);
        Assert(
            timeChart.ZoomBy(2, 1)
            && timeChart.VisibleViewport == new LineChartViewport(start.AddMinutes(1), start.AddMinutes(2))
            && first.VisibleViewport == new LineChartViewport(0, 120), "An incompatible X axis navigates independently");
        second.ViewportGroup = null;
        Assert(
            first.VisibleViewport == new LineChartViewport(0, 60) && second.VisibleViewport is null,
            "Unlinking restores independent bounds and removes the member from the shared extent");

        static LineChartData CreateData(double start, double end) => new([new("Numeric", [new(start, 1), new(end, 3)])]);
    }

    private static void VerifyMemoryAnalysis(Control[] controls)
    {
        var line = controls.OfType<LineChart>().Single(chart => chart.Name == "HeapHistory");
        var buffers = controls.OfType<LineChart>().Single(chart => chart.Name == "BufferHistory");
        var bar = controls.OfType<BarChart>().Single();
        var sunburst = controls.OfType<SunburstChart>().Single();
        var interval = controls.OfType<ComboBox>().Single(item => item.Name == "IntervalBox");
        var overview = controls.OfType<LineChartOverview>().Single();
        var track = Descendants(overview).OfType<Canvas>().Single(canvas => canvas.Name == "PART_Track");
        var trace = track.Children.OfType<ChartPath>().ToArray();
        var window = track.Children.OfType<Rectangle>().Last();
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(overview)!;
        var range = (IRangeValueProvider)peer.GetPattern(PatternInterface.RangeValue)!;
        var data = line.Data;
        Assert(
            line.Mode == LineChartMode.StackedArea
            && line.Data is { IsTimeBased: true }
            && line.Data.Series.Count == 5
            && line.Data.Series.All(series => series.Points.Count == 49),
            "Memory replay renders five heap generations on the same time axis");
        Assert(
            ReferenceEquals(overview.Target, line)
            && trace.Length == 5
            && range.IsReadOnly
            && track.ActualWidth > 0
            && Near(window.Width, track.ActualWidth), "The overview starts with all five series and a full-width window");
        var comparison = bar.Data;
        var extent = line.Data!.Extent!;
        Assert(
            line.ViewportGroup is not null
            && ReferenceEquals(line.ViewportGroup, buffers.ViewportGroup)
            && buffers.Data?.Extent == extent
            && buffers.Data.Series[0].Points.Count == 49
            && line.VisibleViewport == extent
            && buffers.VisibleViewport == extent, "Memory replay opens with the heap and buffers fitted to the same time range");
        var timeSelection = SelectRange(line, 0.75, 0.25);
        Assert(
            timeSelection == new LineChartViewport(extent.Start!.Value.AddSeconds(24), extent.End!.Value.AddSeconds(-24))
            && buffers.SelectedRange == timeSelection
            && line.VisibleViewport == extent
            && buffers.VisibleViewport == extent, "A reversed drag selects the same absolute time range on both timelines");
        buffers.IsSelectionEnabled = false;
        buffers.ClearSelection();
        Assert(
            buffers.SelectedRange is null && line.SelectedRange == timeSelection && buffers.VisibleViewport == line.VisibleViewport,
            "Disabling selection on a linked chart preserves the shared selection and viewport for the group");
        buffers.IsSelectionEnabled = true;
        Assert(buffers.SelectedRange == timeSelection, "Re-enabling synchronized selection restores the current shared range");
        buffers.ClearSelection();
        Assert(line.SelectedRange is null && buffers.SelectedRange is null, "Clearing a linked range clears both timelines");
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
        var preview = typeof(LineChart).GetMethod("SetSelectionPreview", fields)!;
        var bufferPresenter = typeof(LineChart).GetField("_presenter", fields)!.GetValue(buffers)!;
        var bufferOverlay = (FrameworkElement)bufferPresenter.GetType().GetField("_selectionOverlay", fields)!.GetValue(bufferPresenter)!;
        preview.Invoke(line, [timeSelection]);
        Assert(
            buffers.SelectedRange is null && bufferOverlay.Visibility == Visibility.Visible,
            "A drag preview appears on the linked chart before committing");
        buffers.IsSelectionEnabled = false;
        Assert(bufferOverlay.Visibility == Visibility.Collapsed, "Disabling selection hides the linked drag preview");
        buffers.IsSelectionEnabled = true;
        preview.Invoke(line, [null]);
        Assert(bufferOverlay.Visibility == Visibility.Collapsed, "Canceling the drag removes the linked preview");
        SelectRange(buffers, 0.25, 0.75);
        Assert(
            line.ZoomToSelection()
            && line.VisibleViewport == timeSelection
            && line.SelectedRange is null
            && buffers.SelectedRange is null
            && buffers.VisibleViewport == timeSelection
            && ReferenceEquals(bar.Data, comparison),
            "Zooming to a selection updates the buffer chart and preserves the heap comparison interval");
        interval.SelectedIndex = 1;
        var peakPayload = sunburst.Data!.Root.Value;
        Assert(
            line.VisibleViewport?.End - line.VisibleViewport?.Start == TimeSpan.FromSeconds(44)
            && ReferenceEquals(line.Data, data)
            && bar.Data!.Series[0].Values[3] > 0, "The import interval narrows the timeline and shows large object heap growth");
        interval.SelectedIndex = 2;
        Assert(
            line.VisibleViewport?.End - line.VisibleViewport?.Start == TimeSpan.FromSeconds(38)
            && bar.Data!.Series[0].Values[3] < 0
            && sunburst.Data!.Root.Value < peakPayload, "Cleanup updates the timeline, signed heap delta, and retained buffers together");
        var selectedDelta = bar.Data;
        var previousStart = line.VisibleViewport!.Start;
        Assert(
            !range.IsReadOnly
            && Near(range.Value, 100)
            && Near(window.Width, track.ActualWidth * 38 / 96)
            && Near(Canvas.GetLeft(window), track.ActualWidth * 58 / 96),
            "The overview highlights the selected portion of the full session");
        range.SetValue(0);
        Assert(
            line.VisibleViewport!.Start < previousStart
            && line.VisibleViewport.Start == data!.Extent!.Start
            && line.VisibleViewport.End - line.VisibleViewport.Start == TimeSpan.FromSeconds(38)
            && Near(Canvas.GetLeft(window), 0)
            && trace.SequenceEqual(track.Children.OfType<ChartPath>())
            && buffers.VisibleViewport == line.VisibleViewport
            && ReferenceEquals(selectedDelta, bar.Data),
            "Panning the overview moves both charts and preserves the zoom span, preview geometry, and heap comparison");
        Assert(
            buffers.PanBy(0.5)
            && line.VisibleViewport?.Start == extent.Start!.Value.AddSeconds(19)
            && buffers.VisibleViewport == line.VisibleViewport
            && Near(Canvas.GetLeft(window), track.ActualWidth * 19 / 96),
            "Panning the buffer chart updates both the heap chart and overview");
        line.ResetViewport();
        Assert(
            range.IsReadOnly
            && line.VisibleViewport == extent
            && buffers.VisibleViewport == extent
            && Near(window.Width, track.ActualWidth), "Fitting the timeline resets the overview window");
        var resize = typeof(LineChartOverview).GetMethod("SetWindow", fields)!;
        Assert(
            resize.Invoke(overview, [0.25, 1d]) is true
            && line.VisibleViewport == new LineChartViewport(extent.Start.Value.AddSeconds(24), extent.End.Value)
            && buffers.VisibleViewport == line.VisibleViewport
            && Near(window.Width, track.ActualWidth * 0.75),
            "Resizing the full overview window changes the shared span and keeps the opposite end fixed");
        line.ResetViewport();
        var numeric = new LineChart { Data = new LineChartData([new("Samples", [new(0, 1), new(100, 2)])]) };
        overview.Target = numeric;
        numeric.ZoomBy(2);
        range.SetValue(100);
        Assert(
            numeric.VisibleViewport == new LineChartViewport(50, 100)
            && line.VisibleViewport == extent
            && track.Children.OfType<ChartPath>().Count() == 1,
            "Retargeting an overview switches the preview and navigation to a numeric chart");
        overview.Target = null;
        Assert(range.IsReadOnly && track.Children.Count == 0, "Clearing the overview target removes its trace and window");
    }

    private static void VerifyDashboardWidgets(Control[] controls)
    {
        var composition = controls.OfType<BarChart>().Single(chart => chart.Name == "MemorySplit");
        var memory = controls.OfType<LineChart>().Single(chart => chart.Name == "MemoryHistory");
        var latency = controls.OfType<LineChart>().Single(chart => chart.Name == "LatencyHistory");
        var requests = controls.OfType<LineChart>().Single(chart => chart.Name == "RequestsHistory");
        var plot = Plot(composition);
        var marks = GetChartBars(composition);
        Assert(
            composition.Mode == BarChartMode.Stacked100
            && composition.Orientation == Orientation.Horizontal
            && composition.Data?.Categories.Count == 1
            && marks.Length == 4
            && marks.All(mark => mark.Height > 0 && Canvas.GetTop(mark) == Canvas.GetTop(marks[0]))
            && Near(Canvas.GetLeft(marks[0]), 0)
            && Near(Canvas.GetLeft(marks[^1]) + marks[^1].Width, plot.ActualWidth)
            && !plot.Children.OfType<TextBlock>().Any()
            && !plot.Children.OfType<Line>().Any(), "The memory composition is one full-width stack with no axis lines or labels");
        Assert(
            marks.Zip(marks.Skip(1)).All(pair => Near(Canvas.GetLeft(pair.Second) - Canvas.GetLeft(pair.First) - pair.First.Width, 3))
            && marks[0].CornerRadius.Equals(new CornerRadius(4, 0, 0, 4))
            && marks[1..^1].All(mark => mark.CornerRadius.Equals(new CornerRadius(0)))
            && marks[^1].CornerRadius.Equals(new CornerRadius(0, 4, 4, 0)),
            "The memory widget rounds only the stack ends and keeps consistent internal gaps");
        var gap = new Point(Canvas.GetLeft(marks[1]) - 1.5, Canvas.GetTop(marks[1]) + marks[1].Height / 2);
        Assert(ReadChartContext(composition, gap) is null, "The space between segments does not hit a bar");
        var labelBrush = ChartAxisAppearance.GetLabelBrush(latency);
        var timeLabels = Plot(latency).Children.OfType<TextBlock>().ToArray();
        Assert(
            !Plot(memory).Children.OfType<TextBlock>().Any()
            && !Plot(memory).Children.OfType<Line>().Any(line => line.Visibility == Visibility.Visible)
            && labelBrush is not null
            && !ReferenceEquals(latency.Foreground, labelBrush)
            && timeLabels.Length > 0
            && timeLabels.All(label => Math.Abs(label.Opacity - 0.62) < 0.001 && ReferenceEquals(label.Foreground, labelBrush))
            && !Plot(latency).Children.OfType<Line>().Any(line => line.Visibility == Visibility.Visible),
            "Memory history hides both axes and latency keeps only dimmed time labels");
        var requestsPlot = Plot(requests);
        var area = (PathGeometry)requestsPlot.Children.OfType<ChartPath>().Single(path => path.Fill is not null).Data;
        var fill = area.Figures.Single();
        Assert(
            fill.StartPoint == new Point(0, requestsPlot.ActualHeight)
            && ((LineSegment)fill.Segments.Last()).Point == new Point(requestsPlot.ActualWidth, requestsPlot.ActualHeight)
            && !requestsPlot.Children.OfType<TextBlock>().Any(),
            "The docked request trend fills both sides and reaches the bottom of the tile");

        var weekly = controls.OfType<BarChart>().Single(chart => chart.Name == "WeeklyJobs");
        var weeklyPlot = Plot(weekly);
        var columns = GetChartBars(weekly);
        Assert(
            columns.Length == 7
            && Near(Canvas.GetLeft(columns[0]), 0)
            && Near(Canvas.GetLeft(columns[^1]) + columns[^1].Width, weeklyPlot.ActualWidth)
            && columns.All(column => Near(Canvas.GetTop(column) + column.Height, weeklyPlot.ActualHeight))
            && columns.Zip(columns.Skip(1))
                .All(pair => Near(Canvas.GetLeft(pair.Second) - Canvas.GetLeft(pair.First) - pair.First.Width, 2))
            && !weekly.ShowLegend
            && !weeklyPlot.Children.OfType<TextBlock>().Any()
            && !weeklyPlot.Children.OfType<Line>().Any(),
            "The weekly jobs tile docks seven columns to the plot edges with two DIPs between them");
        var sunday = columns[^1];
        var sundayPoint = new Point(Canvas.GetLeft(sunday) + sunday.Width / 2, Canvas.GetTop(sunday) + sunday.Height / 2);
        Assert(
            ReadChartContext(weekly, sundayPoint) is [{ Category: "Sunday", Label: "Completed jobs", Value: 137 }],
            "The weekly tile exposes the day and exact count with its axes hidden");

        var (barText, barColors) = Inspect(composition);
        Assert(
            barText.StartsWith("Installed memory", StringComparison.Ordinal)
            && barText.Contains("Standby", StringComparison.Ordinal)
            && marks.Select(mark => mark.Background).SequenceEqual(barColors),
            "The memory inspector matches each series swatch to the rendered segment and preserves the text readout");
        var (lineText, lineColors) = Inspect(memory);
        Assert(
            lineText.Contains("Used memory", StringComparison.Ordinal)
            && Plot(memory).Children.OfType<ChartPath>().Any(path => path.Stroke is not null && lineColors.SequenceEqual([path.Stroke])),
            "The history inspector swatch matches the plotted series stroke");

        static Canvas Plot(Control chart) => Descendants(chart).OfType<Canvas>().Single(canvas => canvas.Name == "PART_Plot");

        static (string Text, Brush?[] Colors) Inspect(Control chart)
        {
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            var presenter = chart.GetType().GetField("_presenter", fields)!.GetValue(chart)!;
            var inspector = presenter.GetType().GetField("_inspector", fields)!.GetValue(presenter)!;
            var show = inspector.GetType().GetMethod("TryShow", fields)!;
            if (show.Invoke(inspector, [0, null, true, false]) is not true)
            {
                throw new InvalidOperationException("The widget inspector did not open.");
            }

            var card = (ContentControl)inspector.GetType().GetField("_card", fields)!.GetValue(inspector)!;
            var readout = Descendants(card).OfType<ContentPresenter>().Single(item => item.Name == "PART_InspectorReadout");
            var rows = (StackPanel)readout.Content;
            return ((string)card.Content,
                rows.Children.OfType<Grid>().Select(row => row.Children.OfType<Rectangle>().Single().Fill).ToArray());
        }
    }

    private static void VerifySalesPerformance(Control[] controls)
    {
        var revenue = controls.OfType<LineChart>().Single();
        var variance = controls.OfType<BarChart>().Single();
        var returns = controls.OfType<HeatmapChart>().Single();
        var region = controls.OfType<ComboBox>().Single(item => item.Name == "RegionBox");
        var quarter = controls.OfType<ComboBox>().Single(item => item.Name == "PeriodBox");
        Assert(
            revenue.Data?.Series[0].Points.Count == 12
            && variance.Data?.Series.Count == 3
            && returns.Data is { RowCount: 3, ColumnCount: 12 }, "The retail dashboard opens with a full year for all regions");
        region.SelectedIndex = 3;
        quarter.SelectedIndex = 1;
        Assert(
            revenue.Data!.Series.All(item => item.Points.Count == 3)
            && variance.Data!.Series is [{ Name: "Asia Pacific" }]
            && returns.Data!.ColumnCount == 3
            && returns.Data[2, 0] is null, "The region and quarter filters update all retail charts and preserve the unopened channel gap");
        for (var month = 0; month < 3; month++)
        {
            var expected = revenue.Data.Series[0].Points[month].Y!.Value - revenue.Data.Series[1].Points[month].Y!.Value;
            Assert(
                Math.Abs(variance.Data!.Series[0].Values[month]!.Value - expected) < 0.000001,
                "The signed retail variance agrees with the revenue and plan series");
        }

        quarter.SelectedIndex = 2;
        Assert(returns.Data![2, 0] is not null, "Retail returns appear in the first month after launch");
    }

    private static async Task VerifyWebAnalyticsAsync(Page page)
    {
        var tabs = Part<SelectorBar>("ReportTabs");
        var overview = Part<SelectorBarItem>("OverviewTab");
        var acquisition = Part<SelectorBarItem>("AcquisitionTab");
        var engagement = Part<SelectorBarItem>("EngagementTab");
        var purchase = Part<SelectorBarItem>("PurchaseTab");
        var trend = Part<LineChart>("SessionHistory");
        var channels = Part<BarChart>("SessionChannels");
        var checkout = Part<FunnelChart>("CheckoutJourney");
        var hours = Part<HeatmapChart>("VisitorHours");
        var device = Part<ComboBox>("AnalyticsDeviceBox");
        var channel = Part<ComboBox>("AnalyticsChannelBox");
        var picker = Part<RangePicker>("ReportDates");
        var start = PickerPart<CalendarView>("_startCalendar");
        var end = PickerPart<CalendarView>("_endCalendar");
        var compare = Part<ToggleSwitch>("ComparePreviousPeriod");
        var presets = PickerPart<ComboBox>("_presets");
        var error = PickerPart<TextBlock>("_error");
        var rangeSummary = PickerPart<TextBlock>("_rangeSummary");
        var comparisonSummary = Part<TextBlock>("PreviousPeriodSummary");
        var header = PickerPart<ContentPresenter>("_flyoutHeader");
        var footer = PickerPart<ContentPresenter>("_flyoutFooter");

        var source = new RangePicker { Selection = picker.Selection };
        picker.SetBinding(
            RangePicker.SelectionProperty,
            new Binding { Source = source, Path = new PropertyPath(nameof(RangePicker.Selection)), Mode = BindingMode.TwoWay, });

        Assert(
            tabs.SelectedItem == overview && trend.Data!.Series[0].Points.Count == 28 && !Descendants(page).OfType<ExampleCard>().Any(),
            "Web analytics opens as a report workspace without sample cards");
        var metricGrid = Part<Grid>("MetricGrid");
        var tileTrends = Descendants(metricGrid).OfType<LineChart>().ToArray();
        Assert(
            tileTrends.Length == 4
            && tileTrends.All(chart => chart.Data!.Series[0].Points.Count == 28 && !chart.IsHitTestVisible && !chart.ShowLegend),
            "The KPI tiles show lightweight daily trends");
        VerifyTileEdges();
        Assert(
            Part<TextBlock>("SessionsDelta").Style == (Style)page.Resources["MetricGain"]
            && Part<TextBlock>("PurchaseRateDelta").Style == (Style)page.Resources["MetricLoss"],
            "KPI deltas distinguish gains and losses");
        var legend = Descendants(trend).OfType<Grid>().Single(grid => grid.Name == "PART_Legend");
        var swatches = Descendants(legend).OfType<Line>().ToArray();
        Assert(
            swatches.Length == 2
            && (swatches[0].StrokeDashArray?.Count ?? 0) == 0
            && swatches[1].StrokeDashArray.SequenceEqual(new[] { 3d, 2d }), "The line chart legend distinguishes solid and dashed traces");
        Assert(
            metricGrid.Children.OfType<ToggleButton>().Select(item => (Grid.GetRow(item), Grid.GetColumn(item))).Distinct().Count() == 4,
            "Each overview metric occupies its own grid cell");
        var workspace = Part<Grid>("Workspace");
        workspace.Width = 610;
        await WaitForAsync(() => Grid.GetRow(Part<Border>("DeviceSurface")) == 1);
        Assert(
            metricGrid.ColumnDefinitions.Count == 2 && Grid.GetRow(Part<ToggleButton>("PurchaseRateMetric")) == 1,
            "The overview reflows metrics and stacks charts at compact widths");
        VerifyTileEdges();
        workspace.ClearValue(FrameworkElement.WidthProperty);
        workspace.UpdateLayout();
        Part<ToggleButton>("EngagementMetric").IsChecked = true;
        await WaitForAsync(() => trend.ValueFormat == "0.#'%'");
        Assert(
            Part<ToggleButton>("SessionsMetric").IsChecked == false && Part<TextBlock>("TrendHeading").Text == "Daily engagement rate",
            "The KPI toggle selects a percentage trend and clears the previous metric");
        Part<ToggleButton>("SessionsMetric").IsChecked = true;
        await WaitForAsync(() => trend.ValueFormat == "N0");
        var allDevices = await VerifyReportsAsync(28);

        var rangeEnd = new DateTimeOffset(new DateTime(2026, 6, 28, 0, 0, 0, DateTimeKind.Local));
        var original = picker.Selection;
        var pickerButton = PickerPart<DropDownButton>("_button");
        pickerButton.Flyout.ShowAt(pickerButton);
        var interiorDay = rangeEnd.AddDays(-14);
        await WaitForAsync(() =>
            FindDay(start, interiorDay)?.Background == picker.RangeHighlightBrush
            && FindDay(end, interiorDay)?.Background == picker.RangeHighlightBrush);
        Assert(
            header.Visibility == Visibility.Collapsed && footer.Visibility == Visibility.Visible && Descendants(footer).Contains(compare),
            "The flyout displays the supplied footer and collapses an empty header");
        var customHeader = new TextBlock { Text = "Report date options" };
        picker.FlyoutHeader = customHeader;
        await WaitForAsync(() => Descendants(header).Contains(customHeader));
        Assert(header.Visibility == Visibility.Visible, "Adding header content to the open flyout shows it");
        picker.FlyoutHeader = null;
        Assert(header.Content is null && header.Visibility == Visibility.Collapsed, "Clearing header content collapses it");
        Assert(
            FindDay(start, rangeEnd.AddDays(-27))?.Background == picker.RangeEndpointBrush
            && FindDay(end, rangeEnd)?.Background == picker.RangeEndpointBrush
            && end.TransformToVisual(start).TransformPoint(new Point()).X >= start.ActualWidth,
            "The two visible calendars highlight the range and its endpoints side by side");
        Assert(
            rangeSummary.Visibility == Visibility.Visible
            && rangeSummary.Text == $"{rangeEnd.AddDays(-27):d} - {rangeEnd:d} (Days: 28)"
            && comparisonSummary.Visibility == Visibility.Visible
            && comparisonSummary.Text == $"Compared with {rangeEnd.AddDays(-55):d} - {rangeEnd.AddDays(-28):d}",
            "The picker summarizes the dates and inclusive count alongside the previous period");

        presets.SelectedIndex = 0;
        Assert(
            start.SelectedDates.Single().ToLocalTime().Date == rangeEnd.AddDays(-6).Date
            && picker.Selection == original
            && FindDay(start, interiorDay) is { } cleared
            && cleared.Background != picker.RangeHighlightBrush
            && FindDay(end, interiorDay) is { } mirrored
            && mirrored.Background != picker.RangeHighlightBrush, "A preset refreshes both calendars without committing the draft");
        Assert(
            rangeSummary.Text == $"{rangeEnd.AddDays(-6):d} - {rangeEnd:d} (Days: 7)"
            && comparisonSummary.Text == $"Compared with {rangeEnd.AddDays(-13):d} - {rangeEnd.AddDays(-7):d}",
            "The live summary reflects the preset before Apply");
        compare.IsOn = false;
        InvokePickerAction("OnCancel", picker);
        InvokePickerAction("OnOpening", picker);
        Assert(
            start.SelectedDates.Single().ToLocalTime().Date == rangeEnd.AddDays(-27).Date
            && picker.Selection == original
            && compare.IsOn
            && comparisonSummary.Visibility == Visibility.Visible,
            "Reopening after cancellation restores the committed dates and the footer switch");

        SelectDay(start, rangeEnd);
        SelectDay(end, rangeEnd.AddDays(-6));
        InvokePickerAction("OnApply", start);
        Assert(
            rangeSummary.Visibility == Visibility.Collapsed && comparisonSummary.Visibility == Visibility.Collapsed,
            "The picker clears the summary for an inverted range");
        Assert(
            error.Visibility == Visibility.Visible && picker.Selection == original && trend.Data!.Series[0].Points.Count == 28,
            "An inverted date range leaves the current report intact");

        presets.SelectedIndex = 0;
        EventHandler<JPSoftworks.DesktopToolkit.Controls.DateRangeSelectionApplyingEventArgs> reject = (_, args) =>
            args.ErrorMessage = "Choose a different reporting range.";
        picker.SelectionApplying += reject;
        InvokePickerAction("OnApply", start);
        Assert(
            picker.Selection == original && error.Visibility == Visibility.Visible && error.Text == "Choose a different reporting range.",
            "A supplied validation error prevents commit and appears in the flyout");
        picker.SelectionApplying -= reject;
        InvokePickerAction("OnApply", start);
        await WaitForAsync(() => source.Selection == picker.Selection && source.Selection?.Range.DayCount == 7);
        Assert(
            trend.Data!.Series[0].Points.Count == 7
            && picker.Selection?.ComparisonRange?.DayCount == 7
            && tileTrends.All(chart => chart.Data!.Series[0].Points.Count == 7),
            "Applying a range and comparison updates the bound value, report, and tiles together");

        device.SelectedIndex = 1;
        var mobile = await VerifyReportsAsync(7);
        Assert(
            checkout.Data!.Series is [{ Label: "Mobile" }] && mobile > 0 && mobile < allDevices,
            "The device filter applies across reports and narrows the checkout cohort");

        tabs.SelectedItem = acquisition;
        channel.SelectedIndex = 3;
        Assert(
            channels.Data!.Categories is ["Paid search"] && Part<ListView>("ChannelsTable").Items.Count == 1,
            "A channel filter narrows the acquisition chart and detail table");
        tabs.SelectedItem = overview;
        var filtered = trend.Data!.Series[0].Points.Sum(point => point.Y!.Value);
        Assert(
            filtered == channels.Data!.Series[0].Values.Sum() && filtered < mobile,
            "Channel and device filters persist across report navigation");

        compare.IsOn = false;
        Assert(
            comparisonSummary.Visibility == Visibility.Collapsed
            && rangeSummary.Visibility == Visibility.Visible
            && picker.Selection?.ComparisonRange is not null
            && trend.Data!.Series.Count == 2, "Turning off comparison hides its draft summary while the committed report still compares");
        InvokePickerAction("OnApply", compare);
        await WaitForAsync(() => trend.Data!.Series.Count == 1);
        Assert(
            source.Selection == picker.Selection
            && picker.Selection?.ComparisonRange is null
            && trend.Data!.Series[0].Name == "Selected period", "Applying only the switch change removes the previous series");
        tabs.SelectedItem = engagement;
        var search = Part<TextBox>("LandingSearch");
        search.Text = "page-that-does-not-exist";
        await WaitForAsync(() => Part<ListView>("LandingTable").Items.Count == 0);
        Assert(
            Part<TextBlock>("NoLandingPages").Visibility == Visibility.Visible,
            "The landing-page report shows an empty state for unmatched searches");

        SelectDay(start, rangeEnd);
        SelectDay(end, rangeEnd);
        Assert(rangeSummary.Text == $"{rangeEnd:d} (Days: 1)", "A single date uses the same count format as a longer range");
        InvokePickerAction("OnApply", start);
        Assert(
            Enumerable.Range(0, hours.Data!.RowCount).Count(row => hours.Data[row, 0] is not null) == 1,
            "The heatmap leaves weekdays without observations blank in a one-day range");

        var previousTemplate = picker.Template;
        var alternateTemplates = (ResourceDictionary)Microsoft.UI.Xaml.Markup.XamlReader.Load(
            """
            <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                                xmlns:dates="using:JPSoftworks.DesktopToolkit.Controls">
                <ControlTemplate x:Key="AlternateRange" TargetType="dates:CalendarDateRangePicker">
                    <DropDownButton x:Name="PART_Button">
                        <DropDownButton.Flyout>
                            <Flyout>
                                <StackPanel>
                                    <CalendarDatePicker x:Name="PART_StartDate" />
                                    <CalendarDatePicker x:Name="PART_EndDate" />
                                    <Button x:Name="PART_Apply" Content="Commit dates" />
                                </StackPanel>
                            </Flyout>
                        </DropDownButton.Flyout>
                    </DropDownButton>
                </ControlTemplate>
            </ResourceDictionary>
            """);
        picker.Template = (ControlTemplate)alternateTemplates["AlternateRange"];
        picker.ApplyTemplate();
        Assert(
            PickerPart<DropDownButton>("_button").Content is string { Length: > 0 }
            && PickerPart<CalendarDatePicker>("_start").Date?.ToLocalTime().Date == rangeEnd.Date,
            "The replacement picker template receives the committed range");
        PickerPart<CalendarDatePicker>("_start").Date = rangeEnd.AddDays(-1);
        InvokePickerAction("OnApply", picker);
        await WaitForAsync(() => picker.Selection?.Range.DayCount == 2 && source.Selection == picker.Selection);
        Assert(PickerPart<DropDownButton>("_button").Content is string, "A minimal replacement template can commit and display a range");
        picker.Template = previousTemplate;
        picker.ApplyTemplate();
        var restoredButton = PickerPart<DropDownButton>("_button");
        var restoredFooter = PickerPart<ContentPresenter>("_flyoutFooter");
        restoredButton.Flyout.ShowAt(restoredButton);
        await WaitForAsync(() => Descendants(restoredFooter).Contains(compare));
        Assert(footer.Content is null, "Reapplying the template moves the footer content to the new presenter");
        restoredButton.Flyout.Hide();

        static CalendarViewDayItem? FindDay(CalendarView calendar, DateTimeOffset date) =>
            Descendants(calendar).OfType<CalendarViewDayItem>().FirstOrDefault(item => item.Date.ToLocalTime().Date == date.Date);

        static void SelectDay(CalendarView calendar, DateTimeOffset date)
        {
            calendar.SelectedDates.Clear();
            calendar.SelectedDates.Add(date);
        }

        void VerifyTileEdges()
        {
            foreach (var tile in metricGrid.Children.OfType<ToggleButton>())
            {
                var chart = Descendants(tile).OfType<LineChart>().Single();
                var origin = chart.TransformToVisual(tile).TransformPoint(new Point());
                var border = tile.BorderThickness;
                Assert(
                    Math.Abs(origin.X - border.Left) <= 1
                    && Math.Abs(tile.ActualWidth - origin.X - chart.ActualWidth - border.Right) <= 1
                    && Math.Abs(tile.ActualHeight - origin.Y - chart.ActualHeight - border.Bottom) <= 1,
                    "Each tile trend reaches the side and bottom edges inside its border");
            }
        }

        T Part<T>(string name)
            where T : class => (T)page.FindName(name);

        T PickerPart<T>(string name)
            where T : class => (T)typeof(RangePicker).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(picker)!;

        void InvokePickerAction(string name, object sender) =>
            typeof(RangePicker).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(
                picker, [sender, new RoutedEventArgs()]);

        async Task<double> VerifyReportsAsync(int days)
        {
            tabs.SelectedItem = overview;
            var lineData = trend.Data!;
            var total = lineData.Series[0].Points.Sum(point => point.Y!.Value);
            Assert(
                lineData.IsTimeBased
                && lineData.Series.All(series => series.Points.Count == days)
                && lineData.Series[0].Points.Select(point => point.Timestamp)
                    .SequenceEqual(lineData.Series[1].Points.Select(point => point.Timestamp)),
                "The trend aligns the selected and comparison periods by day");

            tabs.SelectedItem = acquisition;
            await WaitForAsync(() => channels.ActualWidth > 0);
            Assert(
                channels.Data!.Series[0].Values.Sum() == total
                && channels.Data.Series[1].Values.Sum() == lineData.Series[1].Points.Sum(point => point.Y!.Value),
                "Channel totals agree with both periods in the overview");

            tabs.SelectedItem = purchase;
            await WaitForAsync(() => checkout.ActualWidth > 0);
            Assert(
                checkout.Data!.Stages[0].Value == total
                && checkout.Data.Series.All(series => series.Values.Zip(series.Values.Skip(1)).All(pair => pair.First >= pair.Second)),
                "Checkout reconciles to the overview and each stage retains only continuing sessions");

            tabs.SelectedItem = engagement;
            await WaitForAsync(() => hours.ActualWidth > 0);
            var averageWeek = 0d;
            for (var row = 0; row < hours.Data!.RowCount; row++)
            {
                for (var column = 0; column < hours.Data.ColumnCount; column++)
                {
                    averageWeek += hours.Data[row, column]!.Value.Value;
                }
            }

            Assert(averageWeek * (days / 7) == total, "The weekday activity heatmap reconciles to the overview");
            tabs.SelectedItem = overview;
            return total;
        }
    }


    private static Line[] GetReferenceLines(Canvas plot) =>
        plot.Children.OfType<Line>().Where(line => line.Clip is RectangleGeometry).ToArray();

    private static void VerifyReferenceLineOptions()
    {
        var references = new[] { new ChartReferenceLine(80, "Budget") };
        var axis = new ChartAxisOptions { ReferenceLines = references };
        references[0] = new ChartReferenceLine(20);
        Assert(
            axis.ReferenceLines.Single() is { Value: 80, Label: "Budget", IsStrokeDashed: true, StrokeThickness: 1 },
            "Reference lines are copied into axis options with a dashed stroke by default");
        Throws<ArgumentOutOfRangeException>(() => new ChartReferenceLine(double.NaN), "Reference coordinates must be finite");
        Throws<ArgumentOutOfRangeException>(
            () => new ChartReferenceLine(80) { StrokeThickness = 0 }, "Reference stroke thickness must be positive");
    }

    private static Canvas? GetMilestoneLayer(Canvas plot) =>
        plot.Children.OfType<Canvas>().SingleOrDefault(layer => layer.Name == "MilestoneLayer");

    private static void VerifyMilestoneContracts()
    {
        var timestamp = new DateTimeOffset(2026, 1, 12, 10, 15, 0, TimeSpan.FromHours(1));
        var source = new[] { new ChartMilestone(timestamp, "Firmware rollout") };
        var milestones = new ChartMilestones(source);
        source[0] = new ChartMilestone(0, "Replacement");
        Assert(
            milestones.Items.Single() is { X: null, Y: null, Timestamp: { } utc, Label: "Firmware rollout" }
            && utc.Offset == TimeSpan.Zero
            && utc.Hour == 9, "Milestone snapshots copy the supplied marks and normalize timestamp coordinates to UTC");
        Throws<ArgumentOutOfRangeException>(() => new ChartMilestone(double.NaN, "Invalid"), "A numeric milestone X must be finite");
        Throws<ArgumentOutOfRangeException>(
            () => new ChartMilestone(1, "Invalid", double.PositiveInfinity), "A numeric milestone Y must be finite when supplied");
    }

    private static async Task VerifyScatterChartGalleryAsync(Control[] controls)
    {
        var chart = controls.OfType<ScatterChart>().Single(item => item.Name == "Requests");
        var subtle = controls.OfType<ScatterChart>().Single(item => item.Name == "SubtleRequests");
        var subtleLabels = Descendants(subtle).OfType<Canvas>().Single(canvas => canvas.Name == "PART_Plot").Children.OfType<TextBlock>()
            .ToArray();
        Assert(
            ReferenceEquals(subtle.Data, chart.Data)
            && !subtle.ShowGrid
            && ChartAxisAppearance.GetPresentation(subtle) == ChartAxisPresentation.Subtle
            && subtleLabels.Length > 0
            && subtleLabels.All(label => label.Opacity < 1),
            "The compact scatter sample shares the data and opens with subtle axes and no gridlines");
        var slider = controls.OfType<Slider>().Single(control => control.Name == "MarkerSizeSlider");
        var plot = Descendants(chart).OfType<Canvas>().Single(canvas => canvas.Name == "PART_Plot");
        var data = chart.Data;
        var originalX = chart.XAxis;
        var originalY = chart.YAxis;
        var references = GetReferenceLines(plot);
        Assert(
            references is [{ IsHitTestVisible: false } horizontal, { IsHitTestVisible: false } vertical]
            && Near(horizontal.Y1, horizontal.Y2)
            && Near(vertical.X1, vertical.X2)
            && plot.Children.OfType<TextBlock>().Any(label => label.Text == "64 KB target"),
            "Scatter renders a horizontal budget and vertical target over the plot without taking pointer input");
        var geometries = plot.Children.OfType<ChartPath>().Select(path => (GeometryGroup)path.Data).ToArray();
        var cached = geometries[0].Children.OfType<EllipseGeometry>().ToArray();
        Assert(
            data?.Series.Count == 2
            && geometries.Length == 2
            && geometries.All(group => group.Children.Count == 11)
            && Near(cached[0].Center.X, cached[3].Center.X)
            && cached[0].Center.Y < cached[3].Center.Y, "Scatter draws independent markers for repeated X values in both series");
        var milestoneLayer = GetMilestoneLayer(plot)!;
        var coldStart = milestoneLayer.Children.OfType<Polygon>().Single();
        var coldStartPoint = ((EllipseGeometry)geometries[1].Children[3]).Center;
        Assert(
            !milestoneLayer.IsHitTestVisible
            && coldStart.Fill is null
            && Near(Canvas.GetLeft(milestoneLayer) + Canvas.GetLeft(coldStart) + coldStart.Points[0].X, coldStartPoint.X)
            && Near(Canvas.GetTop(milestoneLayer) + Canvas.GetTop(coldStart) + coldStart.Points[1].Y, coldStartPoint.Y)
            && milestoneLayer.Children.OfType<TextBlock>().Single().Text == "Cold start",
            "Scatter outlines the annotated data point without taking input from the inspector");
        var labels = plot.Children.OfType<TextBlock>().Select(label => label.Text).ToArray();
        Assert(
            labels.Any(text => text.EndsWith(" KB", StringComparison.Ordinal))
            && labels.Any(text => text.EndsWith(" ms", StringComparison.Ordinal)), "Scatter axes format both numeric dimensions");

        slider.Value = 12;
        await WaitForAsync(() => chart.MarkerSize == 12);
        Assert(
            plot.Children.OfType<ChartPath>().SelectMany(path => ((GeometryGroup)path.Data).Children.OfType<EllipseGeometry>())
                .All(marker => marker.RadiusX == 6 && marker.RadiusY == 6), "The bound marker size updates both scatter series");
        var source = new ScatterChart
        {
            XAxis = new ChartAxisOptions(12, 100, "0' ticks'")
            {
                ReferenceLines = (ChartReferenceLine[])[new(64) { IsStrokeDashed = false, StrokeThickness = 2 }, new(101)],
            },
        };
        BindTo(chart, ScatterChart.XAxisProperty, source, nameof(ScatterChart.XAxis));
        chart.YAxis = new ChartAxisOptions(showLabels: false, showGridLines: false);
        await WaitForAsync(() => chart.XAxis?.Minimum == 12);
        var clipped = plot.Children.OfType<ChartPath>().Sum(path => ((GeometryGroup)path.Data).Children.Count);
        var fixedLabels = plot.Children.OfType<TextBlock>().Select(label => label.Text).ToArray();
        Assert(
            clipped == 14 && fixedLabels.Contains("12 ticks") && fixedLabels.All(text => text.EndsWith(" ticks", StringComparison.Ordinal)),
            "Bound scatter axes crop points and can independently format or hide labels");
        Assert(
            GetReferenceLines(plot) is [{ StrokeThickness: 2 } solid]
            && solid.StrokeDashArray is null or { Count: 0 }
            && solid.Clip is RectangleGeometry clip
            && Near(solid.X1, clip.Rect.Left + clip.Rect.Width * (64 - 12) / (100 - 12)),
            "Scatter uses the fixed axis for bound reference lines and omits out-of-range values");
        chart.ClearValue(ScatterChart.XAxisProperty);
        chart.XAxis = originalX;
        chart.YAxis = originalY;
        chart.Data = null;
        Assert(!plot.Children.OfType<ChartPath>().Any(), "Clearing scatter data removes the marks");
        chart.Data = data;
        slider.Value = 8;
    }

    private static async Task VerifyBindableMetersAsync()
    {
        var color = Color.FromArgb(255, 0, 120, 212);
        var ring = new DoughnutGraph();
        ring.AddSeries("Streaming", color);
        Task.Run(() => ring.SetValues(40)).GetAwaiter().GetResult();
        var ringSource = new DoughnutGraph { Data = new DoughnutGraphData([new("Bound", color)], [17]) };
        BindTo(ring, DoughnutGraph.DataProperty, ringSource, nameof(DoughnutGraph.Data));
        await WaitForAsync(() => ring.Series.FirstOrDefault()?.Name == "Bound");
        await Task.Delay(30);
        var raw = (double[])typeof(DoughnutGraph).GetField("_rawValues", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(ring)!;
        Assert(raw.SequenceEqual([17d]), "A queued producer value cannot overwrite a bound doughnut snapshot");
        ringSource.Data = null;
        await WaitForAsync(() => ring.Series.Count == 0);
        ring.AddSeries("Streaming again", color);
        ring.SetValues(30);
        Assert(ring.Series.Count == 1, "Clearing a bound doughnut restores imperative mode");

        var values = new[] { 26d };
        var usageSource = new VerticalUsageBar { Data = new VerticalUsageBarData([new("Storage", new SolidColorBrush(color))], values) };
        var usage = new VerticalUsageBar { Maximum = 100, Value = 20 };
        BindTo(usage, VerticalUsageBar.DataProperty, usageSource, nameof(VerticalUsageBar.Data));
        values[0] = 50;
        Assert(
            usage.Data?.Values[0] == 26 && usage.Series.Count == 1 && usage.Value == 20,
            "Bound usage bars snapshot their contributions without changing the range value");
        usageSource.Data = null;
        await WaitForAsync(() => usage.Series.Count == 0);
        Assert(usage.Value == 20, "Clearing stacked usage data reveals the single bound value");

        var gauge = new RadialGauge();
        var gaugeSource = new RadialGauge { BandsSource = [new(0, 1, color, "Normal")] };
        BindTo(gauge, RadialGauge.BandsSourceProperty, gaugeSource, nameof(RadialGauge.BandsSource));
        await WaitForAsync(() => gauge.Bands.Count == 1);
        gaugeSource.BandsSource = null;
        await WaitForAsync(() => gauge.Bands.Count == 0);
        Assert(gauge.BandsSource is null, "Gauge threshold bands can be bound, replaced and cleared");

        var graph = new LiveAreaGraph();
        var graphSource = new LiveAreaGraph { HistoryDuration = TimeSpan.FromSeconds(20), ValueRange = new(200, 400) };
        BindTo(graph, LiveAreaGraph.HistoryDurationProperty, graphSource, nameof(LiveAreaGraph.HistoryDuration));
        BindTo(graph, LiveAreaGraph.ValueRangeProperty, graphSource, nameof(LiveAreaGraph.ValueRange));
        BindTo(graph, LiveAreaGraph.IsPausedProperty, graphSource, nameof(LiveAreaGraph.IsPaused));
        graphSource.IsPaused = true;
        await WaitForAsync(() => graph.IsPaused && graph.HistoryDuration == TimeSpan.FromSeconds(20));
        Assert(graph.Minimum == 200 && graph.Maximum == 400, "Both live graph bounds update atomically through binding");
        graphSource.ValueRange = null;
        graphSource.IsPaused = false;
        await WaitForAsync(() => !graph.IsPaused && graph.Maximum == 100);
        Assert(graph.Minimum == 0, "Clearing a bound live range restores its defaults");

        BindTo(graph, LiveAreaGraph.SeriesSourceProperty, graphSource, nameof(LiveAreaGraph.SeriesSource));
        var definitions = new LiveGraphSeries[] { new("CPU", color, color), new("Memory", color, color) };
        graphSource.SeriesSource = new(definitions);
        definitions[0] = new("Replaced", color, color);
        await WaitForAsync(() => graph.Series.Count == 2);
        Assert(graph.Series[0].Name == "CPU", "A bound live series snapshot owns its column definitions");
        await Task.Run(() => graph.AppendSample(42, 68));
        var samples = (System.Collections.ICollection)typeof(LiveAreaGraph).GetField(
            "_samples", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(graph)!;
        Assert(samples.Count == 1, "A producer can append to bound live series");
        graphSource.SeriesSource = new(new("CPU", color, color, 3), new("Memory", color, color));
        await WaitForAsync(() => graph.Series[0].EdgeLineThickness == 3);
        Assert(samples.Count == 1, "Restyling the same live columns retains samples");
        graphSource.SeriesSource = new(new("Memory", color, color), new("CPU", color, color));
        await WaitForAsync(() => graph.Series[0].Name == "Memory");
        Assert(samples.Count == 0, "Changing live column order discards incompatible samples");
        graphSource.SeriesSource = null;
        await WaitForAsync(() => graph.Series.Count == 0);
        graph.AddSeries("Imperative", color, color);
        Assert(graph.Series.Count == 1, "Clearing a bound live schema restores imperative series setup");
    }

    private static void BindTo(FrameworkElement target, DependencyProperty property, object source, string path) =>
        target.SetBinding(property, new Binding { Source = source, Path = new PropertyPath(path), Mode = BindingMode.OneWay });

    private static void VerifyMatrixAndSpiderData()
    {
        var values = new double?[,] { { -4, null }, { 0, 7 } };
        var labels = new string?[] { "Start", "End" };
        var matrix = new HeatmapChartData(values, columnLabels: labels);
        values[0, 0] = 55;
        labels[0] = "Changed";
        Assert(
            matrix[0, 0]?.Value == -4 && matrix[0, 1] is null && matrix[1, 0]?.Value == 0 && matrix.ColumnLabels[0] == "Start",
            "Heatmap snapshots signed values, zero, missing cells and labels");
        Throws<ArgumentOutOfRangeException>(
            () => new HeatmapChartData(new double?[,] { { double.NaN } }), "Heatmap rejects nonfinite measurements");
        var low = Color.FromArgb(255, 0, 0, 0);
        var high = Color.FromArgb(255, 200, 100, 50);
        var stops = new HeatmapColorStop[] { new(0, low), new(4, high) };
        var stepped = new HeatmapColorScale(stops, isStepped: true);
        var linear = new HeatmapColorScale(stops);
        stops[0] = new(0, high);
        Assert(
            stepped.GetColor(0) == low && stepped.GetColor(3) == low && stepped.GetColor(4) == high && linear.GetColor(2).R == 100,
            "Heatmap colors interpolate or change at the configured threshold");

        var axes = new SpiderChartAxis[] { new("A"), new("B"), new("C", 0, 5) };
        var profile = new double?[] { 50, null, 3 };
        var data = new SpiderChartData(axes, [new("Profile", profile)]);
        profile[0] = 17;
        Assert(
            data.Series[0].Values.SequenceEqual((double?[])[50, null, 3]) && data.Axes[2].Maximum == 5,
            "Spider snapshots keep ordered axes, per-axis ranges, and gaps");
        Throws<ArgumentException>(
            () => new SpiderChartData(axes, [new("Incomplete shape", [1, 2])]), "Spider series must contain one entry per axis");
    }

    private static void VerifyChartPalettes()
    {
        var blue = Color.FromArgb(255, 0, 102, 180);
        var red = Color.FromArgb(255, 190, 50, 35);
        var light = new[] { blue };
        var dark = new[] { red };
        var custom = new ChartColorSet(light, dark);
        light[0] = red;
        dark[0] = blue;
        Assert(
            custom.GetColor(2, ElementTheme.Light) == blue && custom.GetColor(2, ElementTheme.Dark) == red,
            "A palette snapshots both variants and cycles by index");
        Assert(
            ChartPalette.Default.GetColor(0) == blue
            && ChartPalette.Default.GetColor(0, ElementTheme.Dark) == Color.FromArgb(255, 99, 174, 244),
            "The default palette preserves the original theme colors");
        Throws<ArgumentException>(() => new ChartColorSet([blue], [red, blue]), "Theme variants require the same number of colors");
        Throws<ArgumentOutOfRangeException>(
            () => ChartPalette.CreateFromAccent(blue, 0), "Generating a palette requires at least one color");

        var generated = ChartPalette.CreateFromAccent(red, 8);
        var repeated = ChartPalette.CreateFromAccent(red, 8);
        Assert(
            generated.LightColors.SequenceEqual(repeated.LightColors)
            && generated.DarkColors.SequenceEqual(repeated.DarkColors)
            && generated.LightColors.Distinct().Count() == generated.Count
            && generated.DarkColors.Distinct().Count() == generated.Count
            && generated.LightColors[0] != generated.DarkColors[0]
            && generated.LightColors[0] != ChartPalette.CreateFromAccent(blue, 1).LightColors[0],
            "Accent generation produces stable, distinct theme variants from the requested hue");

        var neutral = Color.FromArgb(255, 128, 128, 128);
        var ramp = ChartPalette.CreateRamp(neutral, 5);
        Assert(
            ramp.LightColors.Zip(ramp.LightColors.Skip(1)).All(pair => pair.First.R > pair.Second.R)
            && ramp.DarkColors.Zip(ramp.DarkColors.Skip(1)).All(pair => pair.First.R < pair.Second.R),
            "Generated ramps increase intensity on both light and dark backgrounds");
        var thresholds = new[] { 0d, 1d, 3d, 5d, 8d };
        var scale = HeatmapColorScale.FromPalette(ChartPalette.ActivityGreen, thresholds, ElementTheme.Dark, isStepped: true);
        thresholds[2] = 2;
        Assert(
            scale.GetColor(2) == ChartPalette.ActivityGreen.DarkColors[1] && scale.GetColor(3) == ChartPalette.ActivityGreen.DarkColors[2],
            "The activity scale maps the requested thresholds to dark colors");
        Throws<ArgumentException>(
            () => HeatmapColorScale.FromPalette(ramp, [0, 1]), "A palette-based heatmap scale requires one stop per color");
    }

    private static async Task VerifySpiderGalleryAsync(Control[] controls)
    {
        var chart = controls.OfType<SpiderChart>().Single();
        var plot = Descendants(chart).OfType<Canvas>().Single(canvas => canvas.Name == "PART_Plot");
        var toggle = controls.OfType<ToggleSwitch>().Single(control => control.Name == "FillToggle");
        Assert(
            chart.Data?.Axes.Count == 6
            && plot.Children.OfType<TextBlock>().Count() == 6
            && plot.Children.OfType<ChartPath>().Count(path => path.Data is PathGeometry && path.Fill is not null) == 2,
            "Spider renders the two filled profiles and six spoke labels");
        toggle.IsOn = false;
        await WaitForAsync(() => !chart.ShowAreaFill);
        Assert(
            !plot.Children.OfType<ChartPath>().Any(path => path.Data is PathGeometry && path.Fill is not null),
            "The bound Spider fill switch clears polygon fills");
        var original = chart.Data!;
        var source = new SpiderChart { Data = new SpiderChartData(original.Axes.ToArray(), [new("Gap", [15, null, 5, 6, 3, 2])]) };
        toggle.IsOn = true;
        BindTo(chart, SpiderChart.DataProperty, source, nameof(SpiderChart.Data));
        await WaitForAsync(() => chart.ShowAreaFill && chart.Data?.Series.Count == 1);
        var gaps = plot.Children.OfType<ChartPath>().Where(path => path.Data is PathGeometry).Last();
        Assert(
            gaps.Fill is null && ((PathGeometry)gaps.Data).Figures.Count == 4,
            "Replacing bound Spider data leaves a gap on both sides of a missing measurement");
        source.Data = null;
        await WaitForAsync(() => chart.Data is null);
        Assert(plot.Children.Count == 0, "Clearing bound Spider data clears the plot");
    }

    private static async Task VerifyHeatmapGalleryAsync(Control[] controls)
    {
        var chart = controls.OfType<HeatmapChart>().Single(item => item.Name == "Calendar");
        var matrix = controls.OfType<HeatmapChart>().Single(item => item.Name == "Latency");
        var options = controls.OfType<ChartSampleOptions>().Single(item => ReferenceEquals(item.Target, matrix));
        var alignment = (ComboBox)options.FindName("LegendAlignmentBox");
        var originalAlignment = alignment.SelectedIndex;
        alignment.SelectedIndex = 2;
        Assert(
            alignment.Visibility == Visibility.Visible && ChartLegend.GetAlignment(matrix) == HorizontalAlignment.Right,
            "The heatmap exposes its color key alignment in the gallery");
        alignment.SelectedIndex = originalAlignment;
        var tiles = controls.OfType<HeatmapChart>().Single(item => item.Name == "LatencyTiles");
        Assert(
            ReferenceEquals(tiles.Data, matrix.Data)
            && tiles.ShowLegend
            && ChartAxisAppearance.GetPresentation(tiles) == ChartAxisPresentation.Hidden,
            "The latency tiles share the data and retain their color scale with axis labels hidden");
        var data = chart.Data!;
        var dates = new List<DateTimeOffset>();
        for (var column = 0; column < data.ColumnCount; column++)
        {
            for (var row = 0; row < data.RowCount; row++)
            {
                if (data[row, column]?.Timestamp is { } time)
                {
                    dates.Add(time);
                }
            }
        }

        Assert(
            data.RowCount == 7 && data.ColumnCount is 52 or 53 && dates.Count == 365 && dates[^1] - dates[0] == TimeSpan.FromDays(364),
            "The contribution heatmap maps 365 days into week columns");
        var plot = Descendants(chart).OfType<Canvas>().Single(canvas => canvas.Name == "PART_Plot");
        var surface = plot.Children.OfType<CanvasControl>().Single();
        var drawn = false;
        surface.Draw += (_, _) => drawn = true;
        surface.Invalidate();
        await WaitForAsync(() => drawn);
        Assert(
            plot.Children.Count < 25 && chart.ColorScale is { IsStepped: true },
            "A continuous surface draws the calendar cells with few XAML visuals");
        var presenter = typeof(HeatmapChart).GetField("_presenter", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(chart)!;
        var move = presenter.GetType().GetMethod("Move", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var read = presenter.GetType().GetMethod("ReadContext", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var index = 2 * data.ColumnCount + 20;
        var nextRow = (int)move.Invoke(presenter, [index, Windows.System.VirtualKey.Down])!;
        var nextWeek = (int)move.Invoke(presenter, [index, Windows.System.VirtualKey.Right])!;
        var context = ((ChartContextValue[])read.Invoke(presenter, [index])!).Single();
        Assert(
            nextRow == index + data.ColumnCount
            && nextWeek == index + 1
            && context.Timestamp == data[2, 20]?.Timestamp
            && context.Value == data[2, 20]?.Value, "Calendar navigation moves across days and weeks and exposes the raw date and value");
        var source = new HeatmapChart { Data = new HeatmapChartData(new double?[,] { { 0, null, 6 }, { 3, 4, 5 } }) };
        BindTo(chart, HeatmapChart.DataProperty, source, nameof(HeatmapChart.Data));
        await WaitForAsync(() => chart.Data?.ColumnCount == 3);
        Assert(
            (int)move.Invoke(presenter, [0, Windows.System.VirtualKey.Right])! == 2,
            "Heatmap keyboard navigation skips missing cells within a row");
        VerifyOpenHeatmapReadoutRefreshes(presenter);
        source.Data = null;
        await WaitForAsync(() => chart.Data is null);
        Assert(plot.Children.Count == 2, "Clearing a bound Heatmap leaves its blank surface and hidden selection");
    }

    private static void VerifyOpenHeatmapReadoutRefreshes(object presenter)
    {
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
        var inspector = presenter.GetType().GetField("_inspector", fields)!.GetValue(presenter)!;
        var popup = (Popup)inspector.GetType().GetField("_popup", fields)!.GetValue(inspector)!;
        var card = (ContentControl)popup.Child;
        var previous = ToolkitLocalization.StringResourceLoader;
        var loader = new ChartStringLoader();
        try
        {
            loader.Strings["HeatmapRowFormat"] = "R{0}";
            loader.Strings["HeatmapColumnFormat"] = "C{0}";
            loader.Strings["ListPairFormat"] = "First {0}/{1}";
            ToolkitLocalization.StringResourceLoader = loader;
            inspector.GetType().GetMethod("TryShow", fields)!.Invoke(inspector, [2, null, false, false]);
            Assert(popup.IsOpen && card.Content is string first && first.StartsWith("First R1/C3", StringComparison.Ordinal),
                "The open heatmap readout shows the translated cell coordinates");
            loader.Strings["ListPairFormat"] = "Second {1}/{0}";
            ToolkitLocalization.InvalidateStringResources();
            Assert(popup.IsOpen && card.Content is string second && second.StartsWith("Second C3/R1", StringComparison.Ordinal),
                "An open heatmap readout refreshes the same cell without pointer or keyboard input");
        }
        finally
        {
            ToolkitLocalization.StringResourceLoader = previous;
        }
    }

    private static void VerifySankeyGallery(SankeyChart chart)
    {
        var plot = Descendants(chart).OfType<Canvas>().Single(canvas => canvas.Name == "PART_Plot");
        var shapes = plot.Children.OfType<ChartPath>().ToArray();
        var nodes = shapes.Select(shape => shape.Data).OfType<RectangleGeometry>().ToArray();
        var ribbons = shapes.Select(shape => shape.Data).OfType<PathGeometry>().ToArray();
        Assert(
            nodes.Length == 8 && ribbons.Length == 12 && nodes.Select(node => node.Rect.Left).Distinct().Count() == 3,
            "The acquisition Sankey lays out eight nodes and twelve ribbons over three columns");
        var node = nodes[0].Rect;
        var first = ribbons[0].Figures.Single();
        var top = (BezierSegment)first.Segments[0];
        var edge = (LineSegment)first.Segments[1];
        var mid = new Point((first.StartPoint.X + top.Point3.X) / 2, (first.StartPoint.Y + top.Point3.Y + edge.Point.Y - top.Point3.Y) / 2);
        Assert(
            ReadChartContext(chart, new Point(node.Left + node.Width / 2, node.Top + node.Height / 2)) is
                [{ Label: "Search", Value: 640, Item: SankeyChartNode }]
            && ReadChartContext(chart, mid) is [{ Label: "Search to Product page", Value: 490, Item: SankeyChartLink }],
            "Sankey inspection resolves both a node total and the weighted link under its ribbon");

        SankeyChartNode[] keys = [new("a", "A"), new("b", "B")];
        Throws<ArgumentException>(
            () => new SankeyChartData(keys, [new("a", "b", 1), new("b", "a", 1)]), "Sankey data rejects cycles of positive links");
        var original = chart.Data;
        chart.Data = new SankeyChartData(keys, [new("a", "b", 0)]);
        Assert(!plot.Children.OfType<ChartPath>().Any(), "A Sankey snapshot with no active flows clears the plot");
        chart.Data = original;
    }

    private static void VerifyFunnelGallery(Control[] controls)
    {
        var chart = controls.OfType<FunnelChart>().Single(item => item.Name == "Diagram");
        var alignment = controls.OfType<ComboBox>().Single(item => item.Name == "AlignmentBox");
        var taper = controls.OfType<ToggleSwitch>().Single(item => item.Name == "TaperSwitch");
        var radius = controls.OfType<Slider>().Single(item => item.Name == "RadiusSlider");
        var plot = Descendants(chart).OfType<Canvas>().Single(canvas => canvas.Name == "PART_Plot");
        var stages = plot.Children.OfType<ChartPath>().ToArray();
        Assert(
            stages.Length == 5 && plot.Children.OfType<TextBlock>().Any(label => label.Text == "Paid subscriptions"),
            "The funnel renders all five stages with their labels");
        var top = ((PathGeometry)stages[0].Data).Figures.Single();
        Assert(
            top.Segments.OfType<QuadraticBezierSegment>().Count() == 4 && top.Segments.OfType<BezierSegment>().Count() == 1,
            "The side-aligned funnel combines rounded corners with an eased taper");
        var originalRadius = radius.Value;
        radius.Value = 0;
        top = ((PathGeometry)plot.Children.OfType<ChartPath>().First().Data).Figures.Single();
        var topRight = ((LineSegment)top.Segments[0]).Point;
        var bottomLeft = ((LineSegment)top.Segments[2]).Point;
        Assert(
            top.Segments[1] is BezierSegment curve
            && top.Segments[3] is LineSegment
            && curve.Point1.X == topRight.X
            && curve.Point2.X == curve.Point3.X
            && curve.Point3.X - bottomLeft.X < topRight.X - top.StartPoint.X
            && ReadChartContext(chart, new Point(plot.ActualWidth / 2, (top.StartPoint.Y + bottomLeft.Y) / 2)) is
                [{ Label: "Site visits", Value: 24000, CategoryIndex: 0 }],
            "The funnel eases its tapered edge without affecting the first stage readout");
        Assert(bottomLeft.X == top.StartPoint.X, "Left-aligned stages taper along a shared straight left edge");
        alignment.SelectedIndex = 2;
        top = ((PathGeometry)plot.Children.OfType<ChartPath>().First().Data).Figures.Single();
        Assert(
            top.Segments[1] is LineSegment right
            && top.Segments[3] is BezierSegment
            && Near(right.Point.X, ((LineSegment)top.Segments[0]).Point.X),
            "Right-aligned stages taper along a shared straight right edge");
        alignment.SelectedIndex = 1;
        top = ((PathGeometry)plot.Children.OfType<ChartPath>().First().Data).Figures.Single();
        Assert(
            top.Segments[1] is BezierSegment centered
            && top.Segments[3] is BezierSegment
            && Near(
                (top.StartPoint.X + ((LineSegment)top.Segments[0]).Point.X) / 2,
                (centered.Point3.X + ((LineSegment)top.Segments[2]).Point.X) / 2),
            "Centered stages ease both edges around their shared midpoint");
        taper.IsOn = false;
        top = ((PathGeometry)plot.Children.OfType<ChartPath>().First().Data).Figures.Single();
        Assert(!chart.IsTapered && top.Segments.All(segment => segment is LineSegment), "The gallery toggle produces rectangular bands");
        radius.Value = originalRadius;
        top = ((PathGeometry)plot.Children.OfType<ChartPath>().First().Data).Figures.Single();
        Assert(
            top.Segments.OfType<QuadraticBezierSegment>().Count() == 4 && !top.Segments.OfType<BezierSegment>().Any(),
            "Rectangular funnels support rounded corners");
        taper.IsOn = true;
        alignment.SelectedIndex = 0;

        var flow = controls.OfType<FunnelChart>().Single(item => item.Name == "CohortFlow");
        var flowPlot = Descendants(flow).OfType<Canvas>().Single(canvas => canvas.Name == "PART_Plot");
        var geometry = flowPlot.Children.OfType<ChartPath>().Select(path => path.Data).ToArray();
        var ribbons = geometry.OfType<PathGeometry>().ToArray();
        var stops = geometry.OfType<RectangleGeometry>().ToArray();
        Assert(
            flow.Data?.Stages[0].Value == 1620
            && ribbons.Length == 3
            && stops.Length == 5
            && ribbons.All(ribbon => ribbon.Figures.Single().Segments.OfType<BezierSegment>().Count() == 8),
            "Cohort data sums stage totals and draws three continuous ribbons across five stages");
        var closed = stops[^1].Rect;
        Assert(
            ReadChartContext(flow, new Point(closed.Left + closed.Width / 2, closed.Top + closed.Height / 2)) is
                [{ Label: "Closed", Value: 774, CategoryIndex: 4 }], "The flow inspector targets the stage total at the selected column");
        var legend = Descendants(flow).OfType<Grid>().Single(grid => grid.Name == "PART_Legend");
        var cohortLabels = Descendants(legend).OfType<TextBlock>().Where(label => label.Text is "Web" or "Email" or "Chat").ToArray();
        Assert(
            legend.Visibility == Visibility.Visible
            && cohortLabels.Length == 3
            && cohortLabels.All(label =>
                label.TransformToVisual(legend).TransformPoint(new Point(0, label.ActualHeight)).Y <= legend.ActualHeight + 1),
            "The flow shows all three cohort labels within the legend");
        Throws<ArgumentException>(
            () => FunnelChartData.FromSeries(["A", "B"], new FunnelChartSeries("Group", [4])),
            "Funnel data requires every cohort to provide a value at each stage");
        Throws<ArgumentException>(
            () => new FunnelChartData(new FunnelChartStage("Invalid", double.NaN)), "Funnel data rejects non-finite counts");
    }

    private static void VerifyHierarchyChartData()
    {
        var deferred = HierarchyChartNode.CreateDeferredBranch("packages", "Packages", 40);
        var tools = new HierarchyChartNode("tools", "Tools", 8);
        HierarchyChartNode[] children = [deferred, tools];
        var data = new HierarchyChartData(new HierarchyChartNode("workspace", "Workspace", children));
        children[0] = tools;
        Assert(
            data.Root.Value == 48 && data.FindNode("packages") == deferred && deferred.IsDeferred && deferred.Children.Count == 0,
            "A deferred branch contributes its known total before any child nodes exist");

        var loaded = data.WithChildren("packages", new("nuget", "NuGet", 25), new("npm", "npm", 17));
        Assert(
            data.Root.Value == 48
            && deferred.IsDeferred
            && loaded.Root.Value == 50
            && loaded.FindNode("packages") is { Value: 42, IsDeferred: false }
            && loaded.FindNode("nuget") is { Value: 25 }
            && ReferenceEquals(loaded.FindNode("tools"), tools),
            "Loading children updates the ancestor weights and reuses untouched branches");
        Throws<ArgumentException>(
            () => data.WithChildren("packages", new HierarchyChartNode("tools", "Other", 8)),
            "Loading children rejects duplicate keys elsewhere in the hierarchy");
        Throws<ArgumentOutOfRangeException>(
            () => HierarchyChartNode.CreateDeferredBranch("invalid", "Invalid", double.NaN), "Deferred totals must be finite");
        Throws<ArgumentException>(
            () => new HierarchyChartNode(
                "overflow", "Overflow", new HierarchyChartNode("first", "First", double.MaxValue),
                new HierarchyChartNode("second", "Second", double.MaxValue)), "Hierarchy branch totals must be finite");
    }

    private static void VerifyTreemapLayouts()
    {
        HierarchyChartNode[] nodes =
        [
            new("one", "One", 3e-310), new("two", "Two", 1e-310), new("zero", "Zero", 0),
            new("three", "Three", 2e-310), new("four", "Four", 1e-310)
        ];
        var frame = new Rect(9, 17, 160, 100);
        var total = nodes.Sum(node => node.Value);
        const double epsilon = 0.0001;
        Assert(
            TreemapLayouts.Squarified.Arrange(nodes, frame, 1).Select(item => item.NodeIndex).SequenceEqual(new[] { 0, 3, 1, 4 }),
            "Squarified places larger nodes first and keeps the input order of ties");

        foreach (var (name, layout) in new[]
                 {
                     ("Squarified", TreemapLayouts.Squarified), ("Slice and Dice", TreemapLayouts.SliceAndDice),
                     ("Binary", TreemapLayouts.Binary)
                 })
        {
            var placements = layout.Arrange(nodes, frame, 1);
            Assert(
                placements.Select(item => item.NodeIndex).Order().SequenceEqual(new[] { 0, 1, 3, 4 })
                && placements.All(item =>
                    Math.Abs((item.Bounds.Width * item.Bounds.Height) / (frame.Width * frame.Height) - nodes[item.NodeIndex].Value / total)
                    < epsilon), $"{name} produces one tile per positive node with proportional area");
            Assert(
                placements.All(item =>
                    item.Bounds.Left >= frame.Left - epsilon
                    && item.Bounds.Top >= frame.Top - epsilon
                    && item.Bounds.Right <= frame.Right + epsilon
                    && item.Bounds.Bottom <= frame.Bottom + epsilon)
                && placements
                    .SelectMany((first, index) => placements.Skip(index + 1).Select(second => (First: first.Bounds, Second: second.Bounds)))
                    .All(pair => Math.Min(pair.First.Right, pair.Second.Right) - Math.Max(pair.First.Left, pair.Second.Left) <= epsilon
                                 || Math.Min(pair.First.Bottom, pair.Second.Bottom) - Math.Max(pair.First.Top, pair.Second.Top) <= epsilon),
                $"{name} keeps siblings inside their frame without overlap");
        }

        var columns = TreemapLayouts.SliceAndDice.Arrange(nodes, frame, 1);
        var rows = TreemapLayouts.SliceAndDice.Arrange(nodes, frame, 2);
        Assert(
            columns.Select(item => item.NodeIndex).SequenceEqual(new[] { 0, 1, 3, 4 })
            && rows.Select(item => item.NodeIndex).SequenceEqual(new[] { 0, 1, 3, 4 })
            && columns.All(item => Near(item.Bounds.Height, frame.Height))
            && rows.All(item => Near(item.Bounds.Width, frame.Width)),
            "Slice and Dice alternates columns and rows while preserving input order");
        var binary = TreemapLayouts.Binary.Arrange(nodes, frame, 1);
        Assert(
            Near(binary[0].Bounds.Left, binary[1].Bounds.Left)
            && binary[0].Bounds.Top < binary[1].Bounds.Top
            && binary[1].Bounds.Right <= binary[2].Bounds.Left + epsilon, "Binary partitions along the longer side of each subrectangle");
    }

    private static async Task VerifyTreemapGalleryAsync(Control[] controls)
    {
        var chart = controls.OfType<TreemapChart>().Single();
        var layouts = controls.OfType<ComboBox>().Single(item => item.Name == "LayoutBox");
        var levels = controls.OfType<ComboBox>().Single(item => item.Name == "LevelsBox");
        var load = controls.OfType<Button>().Single(item => item.Name == "LoadPackageFoldersButton");
        var plot = Descendants(chart).OfType<Canvas>().Single(canvas => canvas.Name == "PART_Plot");
        var tiles = plot.Children.OfType<Rectangle>().Count(item => item.Fill is not null);
        Assert(
            tiles > 5 && chart.Data?.Root.Value == 144 && chart.Data.FindNode("packages") is { IsDeferred: true, Value: 40 },
            "The folder gallery mixes nested tiles with a deferred package summary");
        Assert(
            chart.TryGetTileBounds("packages", out var packageTile)
            && packageTile.Width > 0
            && packageTile.Left > 0
            && packageTile.Right < chart.ActualWidth
            && !chart.TryGetTileBounds("workspace", out _),
            "Treemap exposes a drill-down anchor in control coordinates only for visible tiles");
        var packageCenter = chart.TransformToVisual(plot).TransformPoint(
            new Point(packageTile.Left + packageTile.Width / 2, packageTile.Top + packageTile.Height / 2));
        Assert(
            ChartContextMenu.GetProvider(chart) is not null
            && ReadChartContext(chart, packageCenter) is [{ Item: HierarchyChartNode { Key: "packages", IsDeferred: true }, Value: 40 }],
            "The Treemap context menu targets the deferred node and its raw value");
        var projectFrame = Tiles()[0];
        var valueOpacity = new Windows.UI.ViewManagement.AccessibilitySettings().HighContrast ? 1 : 0.83;
        Assert(
            chart.TryGetTileBounds("projects", out var projects)
            && chart.TryGetTileBounds("web", out var web)
            && Near(web.Left - projects.Left, 8)
            && projects.Right - web.Right >= 7.9
            && web.Top - projects.Top >= 24
            && projectFrame.RadiusX > 0
            && projectFrame.Stroke is not null
            && plot.Children.OfType<Grid>().Any(grid => Near(Canvas.GetLeft(grid), Canvas.GetLeft(projectFrame) + 8)
                                                        && grid.Children is
                                                            [TextBlock { Text: "Projects", Opacity: 1 }, TextBlock { Text: "54 GB" } total]
                                                        && Math.Abs(total.Opacity - valueOpacity) < 0.001),
            "Treemap frames the parent, gives its total secondary emphasis, and insets children");
        Assert(
            !plot.Children.OfType<Line>().Any()
            && plot.Children.OfType<TextBlock>().Any(label => label.Text == "40 GB" && Math.Abs(label.Opacity - valueOpacity) < 0.001),
            "Treemap uses spacing without header dividers and styles leaf sizes as secondary text");

        var spacing = controls.OfType<Slider>().Single(control => control.Name == "TileSpacingSlider");
        var padding = controls.OfType<Slider>().Single(control => control.Name == "BranchPaddingSlider");
        var corners = controls.OfType<NumberBox>().Single(control => control.Name == "CornerRadiusBox");
        var showValues = controls.OfType<ToggleSwitch>().Single(control => control.Name == "ShowValueLabelsSwitch");
        var opacity = controls.OfType<Slider>().Single(control => control.Name == "ValueLabelOpacitySlider");
        Assert(double.IsNaN(chart.TileCornerRadius), "The empty corner radius editor uses automatic rounding");

        layouts.SelectedIndex = 1;
        spacing.Value = 10;
        padding.Value = 14;
        corners.Value = 0;
        opacity.Value = 0.62;
        await WaitForAsync(() =>
            chart.TileSpacing == 10
            && chart.BranchPadding == 14
            && chart.TileCornerRadius == 0
            && Math.Abs(chart.ValueLabelOpacity - 0.62) < 0.001);
        Assert(
            chart.TryGetTileBounds("projects", out projects)
            && chart.TryGetTileBounds("packages", out packageTile)
            && chart.TryGetTileBounds("web", out web)
            && Near(packageTile.Left - projects.Right, 10)
            && Near(web.Left - projects.Left, 14)
            && Near(projects.Right - web.Right, 14),
            "Treemap spacing and branch padding independently control sibling gaps and child insets");
        Assert(Tiles().All(tile => tile.RadiusX == 0 && tile.RadiusY == 0), "A corner radius of zero squares every tile");
        var opacityWithTheme = valueOpacity == 1 ? 1 : 0.62;
        Assert(
            plot.Children.OfType<TextBlock>().Any(label => label.Text == "40 GB" && Math.Abs(label.Opacity - opacityWithTheme) < 0.001),
            "Treemap applies custom size label opacity with full opacity in contrast themes");

        showValues.IsOn = false;
        await WaitForAsync(() => !chart.ShowValueLabels);
        Assert(
            !Descendants(plot).OfType<TextBlock>().Any(label => label.Text.EndsWith(" GB", StringComparison.Ordinal))
            && Descendants(plot).OfType<TextBlock>().Any(label => label.Text == "Projects"),
            "Turning size labels off keeps folder names visible");

        spacing.Value = 2;
        padding.Value = 8;
        corners.Value = double.NaN;
        showValues.IsOn = true;
        opacity.Value = 0.83;
        layouts.SelectedIndex = 0;
        await WaitForAsync(() =>
            chart.TileSpacing == 2
            && chart.BranchPadding == 8
            && double.IsNaN(chart.TileCornerRadius)
            && chart.ShowValueLabels
            && Math.Abs(chart.ValueLabelOpacity - 0.83) < 0.001);

        levels.SelectedIndex = 0;
        Assert(
            chart.MaxDepth == 1 && plot.Children.OfType<Rectangle>().Count(item => item.Fill is not null) == 5,
            "The level picker collapses the treemap to five top-level folders");
        var squarified = Tiles();
        layouts.SelectedIndex = 1;
        var slices = Tiles();
        Assert(
            chart.Layout == TreemapLayouts.SliceAndDice
            && slices.Length == squarified.Length
            && slices.Select(tile => ((SolidColorBrush)tile.Fill).Color)
                .SequenceEqual(squarified.Select(tile => ((SolidColorBrush)tile.Fill).Color))
            && slices.All(tile => Near(tile.Height + 2, plot.ActualHeight))
            && squarified.Any(tile => !Near(tile.Height + 2, plot.ActualHeight)),
            "The gallery switches to columns while retaining each folder's color");
        layouts.SelectedIndex = 2;
        Assert(
            chart.Layout == TreemapLayouts.Binary && Tiles().Any(tile => !Near(tile.Height + 2, plot.ActualHeight)),
            "The gallery can switch to Binary during layout");
        layouts.SelectedIndex = 1;
        levels.SelectedIndex = 2;
        var beforeLoad = Tiles().Length;
        var invoke = (IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(load)!.GetPattern(PatternInterface.Invoke)!;
        invoke.Invoke();
        await WaitForAsync(() => chart.Data?.FindNode("nuget") is not null);
        Assert(
            chart.Data?.Root.Value == 144
            && chart.Data.FindNode("packages") is { IsDeferred: false, Value: 40 }
            && Tiles().Length > beforeLoad, "Loading package details under Slice and Dice retains the already-known folder size");

        levels.SelectedIndex = 0;
        chart.Layout = new ReverseTreemapLayout();
        var custom = Tiles();
        Assert(
            custom.Length == slices.Length
            && Canvas.GetLeft(custom[0]) > Canvas.GetLeft(custom[^1])
            && ((SolidColorBrush)custom[0].Fill).Color == ((SolidColorBrush)slices[^1].Fill).Color,
            "Treemap honors custom placement order, resolves node indices, and skips invalid or duplicate entries");
        levels.SelectedIndex = 2;
        Assert(Tiles().Length > custom.Length, "Treemap applies the custom strategy to expanded branches");
        layouts.SelectedIndex = 0;
        Assert(chart.Layout is null, "Selecting the default removes the custom strategy");
        chart.BranchPadding = 10_000;
        Assert(Tiles().Length == 5, "Branches use a single tile when the requested padding leaves no room for children");

        Rectangle[] Tiles() => plot.Children.OfType<Rectangle>().Where(item => item.Fill is not null).ToArray();
    }

    private sealed class ReverseTreemapLayout : ITreemapLayout
    {
        public IReadOnlyList<TreemapPlacement> Arrange(IReadOnlyList<HierarchyChartNode> nodes, Rect bounds, int depth)
        {
            var placements = TreemapLayouts.SliceAndDice.Arrange(nodes, bounds, depth).Reverse().ToList();
            placements.Insert(0, new TreemapPlacement(-1, bounds));
            placements.Insert(1, new TreemapPlacement(0, Rect.Empty));
            placements.Add(placements[2]);
            return placements;
        }
    }

    private static async Task VerifyIcicleGalleryAsync(Control[] controls)
    {
        var chart = controls.OfType<IcicleChart>().Single();
        var direction = controls.OfType<ToggleSwitch>().Single(item => item.Name == "OrientationSwitch");
        var load = controls.OfType<Button>().Single(item => item.Name == "LoadPackageFoldersButton");
        var plot = Descendants(chart).OfType<Canvas>().Single(canvas => canvas.Name == "PART_Plot");
        var values = controls.OfType<ToggleSwitch>().Single(item => item.Name == "ValueLabelsSwitch");
        Assert(
            chart.ShowValueLabels && plot.Children.OfType<TextBlock>().Any(label => label.Text.EndsWith(" GB", StringComparison.Ordinal)),
            "The default icicle presentation includes value labels inside large tiles");
        values.IsOn = false;
        await WaitForAsync(() => !chart.ShowValueLabels);
        Assert(
            plot.Children.OfType<TextBlock>().Any()
            && !plot.Children.OfType<TextBlock>().Any(label => label.Text.EndsWith(" GB", StringComparison.Ordinal)),
            "The icicle can hide tile values while retaining node names");
        values.IsOn = true;
        var vertical = Tiles();
        Assert(
            chart.Orientation == Orientation.Vertical
            && vertical.Length == 17
            && Near(vertical[0].Width + 2, plot.ActualWidth)
            && Near(vertical[1].Width + 2, plot.ActualWidth * 0.375)
            && Near(Canvas.GetLeft(vertical[1]), Canvas.GetLeft(vertical[2]))
            && Canvas.GetTop(vertical[2]) > Canvas.GetTop(vertical[1]),
            "The vertical icicle aligns a weighted branch and its children on successive rows");

        direction.IsOn = true;
        var horizontal = Tiles();
        Assert(
            chart.Orientation == Orientation.Horizontal
            && horizontal.Length == vertical.Length
            && Near(horizontal[0].Height + 2, plot.ActualHeight)
            && Near(horizontal[1].Height + 2, plot.ActualHeight * 0.375)
            && Near(Canvas.GetTop(horizontal[1]), Canvas.GetTop(horizontal[2]))
            && Canvas.GetLeft(horizontal[2]) > Canvas.GetLeft(horizontal[1]),
            "The horizontal icicle advances levels by column and sizes branches along the vertical axis");

        const int packages = 9;
        Assert(
            chart.Data?.FindNode("packages") is { IsDeferred: true, Value: 40 }
            && Near(horizontal[packages].Height + 2, plot.ActualHeight * (40d / 144)),
            "The package branch occupies its known share while details are deferred");
        Assert(
            chart.TryGetTileBounds("packages", out var packageTile)
            && Near(packageTile.Height, horizontal[packages].Height)
            && packageTile.X > Canvas.GetLeft(horizontal[packages])
            && !chart.TryGetTileBounds("nuget", out _), "Icicle exposes the horizontal tile anchor before deferred children arrive");
        var invoke = (IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(load)!.GetPattern(PatternInterface.Invoke)!;
        invoke.Invoke();
        await WaitForAsync(() => chart.Data?.FindNode("nuget") is not null);
        var expanded = Tiles();
        Assert(
            chart.Data?.Root.Value == 144
            && chart.Data.FindNode("packages") is { IsDeferred: false, Value: 40 }
            && expanded.Length == horizontal.Length + 2
            && Near(expanded[packages].Height, horizontal[packages].Height)
            && Near(expanded[packages + 1].Height + 2, plot.ActualHeight * (24d / 144)),
            "Loading details adds proportional child tiles and preserves the package span");

        chart.MaxDepth = 1;
        Assert(Tiles().Length == 6, "The icicle depth limit keeps the root and the five top-level folders");

        Rectangle[] Tiles() => plot.Children.OfType<Rectangle>().Where(item => item.Fill is not null).ToArray();
    }

    private static async Task VerifySunburstGalleryAsync(Control[] controls)
    {
        var chart = controls.OfType<SunburstChart>().Single();
        var depth = controls.OfType<ComboBox>().Single(item => item.Name == "LevelsBox");
        var rotation = controls.OfType<Slider>().Single(item => item.Name == "StartAngleSlider");
        var load = controls.OfType<Button>().Single(item => item.Name == "LoadPackageFoldersButton");
        var plot = Descendants(chart).OfType<Canvas>().Single(canvas => canvas.Name == "PART_Plot");
        var initial = Segments();
        Assert(
            initial.Length == 17
            && initial[0].Data is EllipseGeometry
            && chart.StartAngle == -90
            && chart.Data?.FindNode("packages") is { IsDeferred: true, Value: 40 },
            "The folder Sunburst displays a root disk, three levels of segments, and a deferred branch");
        var presenter = typeof(SunburstChart).GetField("_presenter", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(chart)!;
        var inspector = presenter.GetType().GetField("_inspector", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(presenter)!;
        var show = inspector.GetType().GetMethod("TryShow", BindingFlags.Instance | BindingFlags.NonPublic)!;
        object?[] selection = [9, null, true, false];
        Assert(
            show.Invoke(inspector, selection) is true
            && selection[1] is string readout
            && readout.StartsWith("Workspace / Packages", StringComparison.Ordinal)
            && readout.Contains("Child details pending", StringComparison.Ordinal)
            && plot.Children.OfType<ChartPath>().Count(path => path.Fill is null && path.Visibility == Visibility.Visible) == 2,
            "Inspecting a deferred Sunburst segment outlines it and reports its pending children");
        ChartInspection.SetIsEnabled(chart, false);
        ChartInspection.SetIsEnabled(chart, true);
        var center = ((EllipseGeometry)initial[0].Data).Center;
        var projects = Figure(initial[1]);
        var projectEnd = (ArcSegment)projects.Segments[0];
        var projectLabel = RingLabels().First();
        var radius = projectEnd.Size.Width;
        var innerRadius = ((ArcSegment)projects.Segments[2]).Size.Width;
        Assert(
            projectLabel.Data is PathGeometry { Figures.Count: > 1 } text
            && text.Figures.SelectMany(figure => ((PolyLineSegment)figure.Segments[0]).Points).All(point =>
            {
                var distance = Math.Sqrt(Math.Pow(point.X - center.X, 2) + Math.Pow(point.Y - center.Y, 2));
                return distance > innerRadius
                       && distance < radius
                       && Angle(point) > Angle(projects.StartPoint)
                       && Angle(point) < Angle(projectEnd.Point);
            })
            && plot.Children.OfType<StackPanel>().Single().Children.OfType<TextBlock>()
                .All(label => label.FontWeight.Weight == Microsoft.UI.Text.FontWeights.Normal.Weight),
            "Sunburst draws a regular center label and keeps the curved project label inside its sector");
        var web = Figure(initial[2]);
        var webEnd = (ArcSegment)web.Segments[0];
        Assert(
            webEnd.Size.Width > projectEnd.Size.Width
            && Math.Abs(Angle(projects.StartPoint) + 90) < 1
            && Math.Abs(Angle(projectEnd.Point) - Angle(projects.StartPoint) - 135) < 2
            && Math.Abs(Angle(web.StartPoint) + 90) < 1
            && Math.Abs(Angle(webEnd.Point) - Angle(web.StartPoint) - 65) < 2,
            "Ring radius encodes depth and segment angles match parent and child weights");

        rotation.Value = 0;
        await WaitForAsync(() => chart.StartAngle == 0);
        Assert(Math.Abs(Angle(Figure(Segments()[1]).StartPoint)) < 1, "Changing the start angle rotates the first segment");
        depth.SelectedIndex = 0;
        Assert(
            chart.MaxDepth == 1 && Segments().Length == 6 && !chart.TryGetTileBounds("web", out _),
            "The depth picker keeps the root and first ring");
        depth.SelectedIndex = 2;
        Assert(chart.TryGetTileBounds("packages", out var deferredBounds), "A deferred branch has a visible zoom anchor");
        var invoke = (IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(load)!.GetPattern(PatternInterface.Invoke)!;
        invoke.Invoke();
        await WaitForAsync(() => chart.Data?.FindNode("nuget") is not null);
        Assert(
            Segments().Length == initial.Length + 2
            && chart.TryGetTileBounds("nuget", out _)
            && chart.TryGetTileBounds("packages", out var loadedBounds)
            && loadedBounds.Equals(deferredBounds), "Loading children adds segments and preserves the parent wedge");

        rotation.Value = -90;
        await WaitForAsync(() => chart.StartAngle == -90);
        chart.Data = new HierarchyChartData(FolderSizeSample.Summary.FindNode("build")!);
        var build = Segments();
        var debug = Figure(build[1]);
        var release = Figure(build[2]);
        var top = -Math.PI / 2;
        var split = top + Math.Tau * 16 / 26;
        Assert(
            IsOneDipGap(debug.StartPoint, ((ArcSegment)release.Segments[0]).Point, top)
            && IsOneDipGap(((ArcSegment)debug.Segments[2]).Point, ((LineSegment)release.Segments[1]).Point, top)
            && IsOneDipGap(release.StartPoint, ((ArcSegment)debug.Segments[0]).Point, split)
            && IsOneDipGap(((ArcSegment)release.Segments[2]).Point, ((LineSegment)debug.Segments[1]).Point, split),
            "Both Build output separators are one DIP wide at the inner and outer edges");
        var hitTest = presenter.GetType().GetMethod("HitTest", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var buildOuterRadius = ((ArcSegment)debug.Segments[0]).Size.Width;
        var buildInnerRadius = ((ArcSegment)debug.Segments[2]).Size.Width;
        Assert(
            HitAt(0, buildInnerRadius + 2) == -1
            && HitAt(0, buildOuterRadius - 2) == -1
            && HitAt(0.65, buildInnerRadius + 2) == 1
            && HitAt(-0.65, buildOuterRadius - 2) == 2, "Inspection follows the parallel slice edges near both ends of a separator");

        chart.Data = new HierarchyChartData(new HierarchyChartNode("root", "Root", new HierarchyChartNode("only", "Only", 1)));
        Assert(
            Segments().Length == 2 && Segments()[1].Data is GeometryGroup { Children.Count: 2, FillRule: FillRule.EvenOdd },
            "A single full-width child uses a complete ring with the center cut out");
        chart.Data = new HierarchyChartData(
            new HierarchyChartNode(
                "root", "Root", new HierarchyChartNode("wide", "Wide", 1000), new HierarchyChartNode("tiny", "Tiny", 1)));
        Assert(
            Segments().Length == 3 && RingLabels().Length == 1 && chart.TryGetTileBounds("tiny", out _),
            "Sunburst hides a label in a narrow sector and retains the sector for inspection");
        chart.Data = null;
        Assert(Segments().Length == 0 && !chart.TryGetTileBounds("root", out _), "Clearing the hierarchy removes all segments");

        int HitAt(double offset, double radius) =>
            (int)hitTest.Invoke(presenter, [new Point(center.X + offset, center.Y - radius)])!;

        double Angle(Point point) => Math.Atan2(point.Y - center.Y, point.X - center.X) * 180 / Math.PI;

        static bool IsOneDipGap(Point first, Point second, double axis) =>
            Math.Abs(Math.Abs((second.X - first.X) * Math.Sin(axis) - (second.Y - first.Y) * Math.Cos(axis)) - 1) < 0.002;

        ChartPath[] Segments() => plot.Children.OfType<ChartPath>().Where(path => path.Fill is not null).ToArray();
        ChartPath[] RingLabels() => plot.Children.OfType<Canvas>().Single().Children.OfType<ChartPath>().ToArray();
        static PathFigure Figure(ChartPath path) => ((PathGeometry)path.Data).Figures.Single();
    }

    private static void VerifyLiveHistoryBoundaries()
    {
        var graph = new LiveAreaGraph { TransitionDuration = TimeSpan.Zero };
        var color = Color.FromArgb(255, 0, 120, 212);
        graph.AddSeries("Signal", color, color);
        var first = DateTimeOffset.MinValue;
        graph.AppendSample(first, 10);
        graph.HistoryDuration = TimeSpan.MaxValue;
        graph.AppendSample(DateTimeOffset.UnixEpoch, 20);

        var read = typeof(LiveAreaGraph).GetMethod(
            "TryGetValuesAtTimeCore", BindingFlags.NonPublic | BindingFlags.Instance, binder: null,
            [typeof(DateTimeOffset), typeof(double[]).MakeByRefType()], modifiers: null)!;
        object?[] args = [first, null];
        Assert((bool)read.Invoke(graph, args)! && ((double[])args[1]!)[0] == 10, "Maximum history retains the earliest sample");
        args[0] = DateTimeOffset.UnixEpoch;
        Assert(
            (bool)read.Invoke(graph, args)! && ((double[])args[1]!)[0] == 20, "Appending after the earliest sample preserves new values");

        var inspect = typeof(LiveAreaGraph).GetMethod("GetInspectionTimestampCore", BindingFlags.NonPublic | BindingFlags.Static)!;
        var end = DateTimeOffset.MaxValue;
        Assert(
            (DateTimeOffset)inspect.Invoke(null, [0d, 1d, end, TimeSpan.MaxValue])! == first,
            "Inspection clamps the left edge to the earliest instant");
        Assert(
            (DateTimeOffset)inspect.Invoke(null, [1d, 1d, end, TimeSpan.MaxValue])! == end, "Inspection reaches the exact latest instant");
    }

    private static void VerifyLiveSmoothing()
    {
        var graph = new LiveAreaGraph { Interpolation = LiveGraphInterpolation.Smooth };
        var color = Color.FromArgb(255, 0, 120, 212);
        graph.AddSeries("Signal", color, color);
        var read = typeof(LiveAreaGraph).GetMethod(
            "TryGetValuesAtTimeCore", BindingFlags.NonPublic | BindingFlags.Instance, binder: null,
            [typeof(DateTimeOffset), typeof(double[]).MakeByRefType()], modifiers: null)!;
        var origin = DateTimeOffset.UtcNow.AddSeconds(-5);

        double At(int milliseconds)
        {
            object?[] args = [origin.AddMilliseconds(milliseconds), null];
            return (bool)read.Invoke(graph, args)! ? ((double[])args[1]!)[0] : double.NaN;
        }

        graph.AppendSample(origin, 0);
        graph.AppendSample(origin.AddMilliseconds(500), 30);
        var history = At(900);
        var previous = At(999);
        var current = At(1000);
        graph.AppendSample(origin.AddMilliseconds(1000), 60);
        var next = At(1001);
        Assert(At(900) == history && At(1000) == current, "Retargeting the smoothed live plot preserves earlier values and the join");
        Assert(
            next > current && current > previous && Math.Abs((next - current) - (current - previous)) < 0.001,
            "The smoothed live plot carries its slope into a continuing trend");

        var reversal = At(1150);
        graph.AppendSample(origin.AddMilliseconds(1150), 10);
        Assert(
            At(1150) == reversal
            && Enumerable.Range(0, 10).All(index => At(1150 + index * 50) >= 10 && At(1150 + index * 50) <= reversal)
            && At(1600) == 10, "A reversed trend stays between the plotted value and the new target");

        graph.Interpolation = LiveGraphInterpolation.Eased;
        Assert(At(950) == 30, "Changing the interpolation mode redraws retained samples");
        graph.Interpolation = LiveGraphInterpolation.Smooth;
        graph.TransitionDuration = TimeSpan.Zero;
        graph.AppendSample(origin.AddMilliseconds(1150), 77);
        Assert(At(1150) == 77, "Zero duration still applies immediate samples at duplicate timestamps");
    }

    private static async Task VerifyLineChartGalleryAsync(Control[] controls)
    {
        var charts = controls.OfType<LineChart>().ToArray();
        var numeric = charts.Single(chart => chart.Name == "NumericChart");
        var compact = charts.Single(chart => chart.Name == "CompactLatencyChart");
        Assert(
            !compact.ShowLegend && ChartAxisAppearance.GetPresentation(compact) == ChartAxisPresentation.Hidden,
            "The compact preview applies the documented style");
        var overrides = new SolidColorBrush(Color.FromArgb(255, 88, 22, 144));
        var borderBrush = numeric.BorderBrush;
        ChartAxisAppearance.SetGridLineBrush(numeric, overrides);
        await WaitForAsync(() => GetLinePlot(numeric).Children.OfType<Line>().Any(line => ReferenceEquals(line.Stroke, overrides)));
        Assert(
            ReferenceEquals(numeric.BorderBrush, borderBrush), "The Cartesian grid brush changes grid strokes independently of the frame");
        var numericPlot = GetLinePlot(numeric);
        var labelsSwitch = controls.OfType<ToggleSwitch>().Single(toggle => toggle.Name == "YAxisLabelsSwitch");

        bool HasAxisLabels(TextAlignment alignment) =>
            numericPlot.Children.OfType<TextBlock>().Any(label => label.TextAlignment == alignment);

        Assert(HasAxisLabels(TextAlignment.Right), "The latency chart initially shows Y-axis labels");
        labelsSwitch.IsOn = false;
        await WaitForAsync(() => !HasAxisLabels(TextAlignment.Right));
        Assert(
            HasAxisLabels(TextAlignment.Center)
            && numericPlot.Children.OfType<Line>().Any(line => ReferenceEquals(line.Stroke, overrides))
            && GetReferenceLines(numericPlot).Length == 1,
            "The latency chart can hide Y-axis labels while keeping horizontal labels, grid lines, and its budget");
        labelsSwitch.IsOn = true;
        await WaitForAsync(() => HasAxisLabels(TextAlignment.Right));
        numeric.ClearValue(ChartAxisAppearance.GridLineBrushProperty);

        var customReadouts = controls.OfType<ToggleSwitch>().Single(item => item.Name == "CustomReadoutsSwitch");
        customReadouts.IsOn = true;
        await WaitForAsync(() => Descendants(numeric).OfType<ContentPresenter>().Any(presenter =>
            presenter.Content is ChartLegendItem { Name.Length: > 0, Fill: not null }));
        Assert(ChartInspection.GetContentTemplate(numeric) is not null, "The gallery installs custom legend and inspector templates");
        customReadouts.IsOn = false;
        var time = charts.Single(chart => chart.Name == "TemperatureChart");
        Assert(
            numeric.Data is { IsTimeBased: false, Series.Count: 2 } && time.Data is { IsTimeBased: true, Series.Count: 2 },
            "The gallery binds two independent series to both the numeric and time line charts");
        Assert(
            ReferenceEquals(compact.Data, numeric.Data)
            && compact.Interpolation == LineChartInterpolation.Monotone
            && compact.ShowAreaFill
            && GetLinePlot(compact).Children.OfType<TextBlock>().Select(label => label.Text).SequenceEqual(new[] { "80 ms budget" })
            && GetReferenceLines(GetLinePlot(compact)).Length == 1,
            "The compact latency chart keeps its reference and label visible with axes hidden");
        var budgetSwitch = controls.OfType<ToggleSwitch>().Single(toggle => toggle.Name == "LatencyBudgetSwitch");
        Assert(GetReferenceLines(GetLinePlot(numeric)).Length == 1, "The numeric line chart starts with a latency budget");
        budgetSwitch.IsOn = false;
        await WaitForAsync(() => numeric.YAxis is null);
        Assert(GetReferenceLines(GetLinePlot(numeric)).Length == 0, "Replacing bound axis options removes the budget");
        budgetSwitch.IsOn = true;
        await WaitForAsync(() => numeric.YAxis is not null);
        var numericPaths = GetLinePlot(numeric).Children.OfType<ChartPath>().ToArray();
        var numericLines = numericPaths.Where(path => path.Stroke is not null).ToArray();
        Assert(
            numeric.Interpolation == LineChartInterpolation.Linear && numericLines.Length == 2 && numericPaths.Length == 2,
            "The numeric line chart renders two series without area fill by default");
        var runs = ((PathGeometry)numericLines[0].Data).Figures;
        Assert(runs.Count == 2 && runs.All(run => !run.IsClosed), "A gap creates two disconnected line figures");
        var origin = runs[0].StartPoint.X;
        var width = ((PolyLineSegment)runs[1].Segments[0]).Points[0].X - origin;
        Assert(
            width > 0
            && Near(((PolyLineSegment)runs[0].Segments[0]).Points[0].X - origin, width * 0.10)
            && Near(runs[1].StartPoint.X - origin, width * 0.70), "Numeric X spacing follows the supplied coordinates");
        VerifyLineInspectorGuide(numeric, origin, origin + width);

        Assert(
            numeric.IsViewportInteractionEnabled
            && numeric.IsSelectionEnabled
            && !numeric.IsZoomEnabled
            && (numericPlot.ManipulationMode & Microsoft.UI.Xaml.Input.ManipulationModes.Scale) == 0,
            "Plot zoom defaults off even when the chart supports viewport gestures and range selection");
        numeric.IsZoomEnabled = true;
        Assert(
            (numericPlot.ManipulationMode & Microsoft.UI.Xaml.Input.ManipulationModes.Scale) != 0,
            "Enabling plot zoom enables pinch input on a loaded chart");
        numeric.IsZoomEnabled = false;
        Assert(
            (numericPlot.ManipulationMode & Microsoft.UI.Xaml.Input.ManipulationModes.Scale) == 0,
            "Disabling plot zoom restores the plot manipulation mode");

        var rangeSelection = controls.OfType<ToggleSwitch>().Single(toggle => toggle.Name == "RangeSelectionSwitch");
        SelectRange(numeric, 0.25, 0.75);
        rangeSelection.IsOn = false;
        await WaitForAsync(() => !numeric.IsSelectionEnabled);
        Assert(
            numeric.SelectedRange is null && numeric.VisibleViewport is null,
            "The selection toggle clears the selected range and preserves the full viewport");
        rangeSelection.IsOn = true;
        await WaitForAsync(() => numeric.IsSelectionEnabled);

        numeric.ShowAreaFill = true;
        var fill = GetLinePlot(numeric).Children.OfType<ChartPath>().First(path => path.Fill is not null);
        var fillRuns = ((PathGeometry)fill.Data).Figures;
        Assert(
            fillRuns.Count == 2 && fillRuns.All(run => run.IsClosed && run.IsFilled),
            "Area fill closes each numeric run separately across a gap");
        numeric.ShowAreaFill = false;

        var timePaths = GetLinePlot(time).Children.OfType<ChartPath>().ToArray();
        var timeLines = timePaths.Where(path => path.Stroke is not null).Select(path => (PathGeometry)path.Data).ToArray();
        var timeFills = timePaths.Where(path => path.Fill is not null).Select(path => (PathGeometry)path.Data).ToArray();
        Assert(
            time.Interpolation == LineChartInterpolation.Linear
            && timeLines.Length == 2
            && timeFills.Length == 2
            && timeLines[1].Figures.Count == 2
            && timeFills[1].Figures.Count == 2, "Time series start with straight lines and separate fill figures on each side of a gap");
        var timeFigure = timeLines[0].Figures.Single();
        var timePoints = ((PolyLineSegment)timeFigure.Segments[0]).Points;
        var timeWidth = timePoints[^1].X - timeFigure.StartPoint.X;
        Assert(
            timeWidth > 0
            && Near(timePoints[0].X - timeFigure.StartPoint.X, timeWidth / 6)
            && Near(timePoints[1].X - timeFigure.StartPoint.X, timeWidth * 2 / 3), "Irregular timestamps are spaced by elapsed time");
        var timeLabels = GetLinePlot(time).Children.OfType<TextBlock>().Select(label => label.Text).ToArray();
        var start = time.Data!.Series[0].Points[0].Timestamp!.Value.ToUniversalTime();
        Assert(
            timeLabels.Contains(start.ToString("HH:mm", CultureInfo.CurrentCulture))
            && timeLabels.Contains(start.AddHours(1).ToString("HH:mm", CultureInfo.CurrentCulture)),
            "Time-axis labels use the configured format and UTC");
        VerifyMalformedDateTickFormats(time, start);
        VerifyCalendarRangeTickFormats(time);

        time.XAxis = new ChartAxisOptions { ReferenceLines = (ChartReferenceLine[])[new(start.AddMinutes(30), "Checkpoint"), new(5)], };
        Assert(
            GetReferenceLines(GetLinePlot(time)) is [var checkpoint]
            && Near(checkpoint.X1, timeFigure.StartPoint.X + timeWidth / 2)
            && Near(checkpoint.X1, checkpoint.X2), "A UTC reference line follows the elapsed time axis and ignores numeric coordinates");
        time.Viewport = new LineChartViewport(start, start.AddMinutes(20));
        await WaitForAsync(() => GetReferenceLines(GetLinePlot(time)).Length == 0);
        Assert(time.VisibleViewport?.End == start.AddMinutes(20), "A time reference beyond the viewport vanishes when zooming");
        time.Viewport = null;
        time.XAxis = null;

        var milestoneLayer = GetMilestoneLayer(GetLinePlot(time))!;
        var milestoneMarkers = milestoneLayer.Children.OfType<Polygon>().ToArray();
        Assert(
            milestoneMarkers.Length == 2
            && milestoneLayer.Children.OfType<Line>().Count() == 2
            && Near(
                Canvas.GetLeft(milestoneLayer) + Canvas.GetLeft(milestoneMarkers[0]) + milestoneMarkers[0].Points[0].X,
                timeFigure.StartPoint.X + timeWidth / 4), "Time milestones mark UTC events along the plot edge, including between samples");
        time.Viewport = new LineChartViewport(start.AddMinutes(25), start.AddMinutes(55));
        await WaitForAsync(() => GetMilestoneLayer(GetLinePlot(time))?.Children.OfType<Polygon>().Count() == 1);
        Assert(
            GetMilestoneLayer(GetLinePlot(time))?.Children.OfType<TextBlock>().Single().Text == "Fan curve adjusted",
            "The time viewport excludes milestone markers outside the zoomed interval");
        time.Viewport = null;
        var milestoneSwitch = controls.OfType<ToggleSwitch>().Single(toggle => toggle.Name == "MilestoneSwitch");
        milestoneSwitch.IsOn = false;
        await WaitForAsync(() => time.Milestones is null);
        Assert(GetMilestoneLayer(GetLinePlot(time)) is null, "A bound milestone snapshot can be cleared");
        milestoneSwitch.IsOn = true;
        await WaitForAsync(() => time.Milestones?.Items.Count == 2);

        var interpolationSwitch = controls.OfType<ToggleSwitch>().Single(toggle => toggle.Name == "InterpolationSwitch");
        VerifyMonotoneGallery(time, interpolationSwitch, timeLines);
        VerifyMonotoneNumericEdgeCases(numeric);

        var original = numeric.Data;
        var source = new LineChart { Data = original };
        numeric.SetBinding(
            LineChart.DataProperty,
            new Binding { Source = source, Path = new PropertyPath(nameof(LineChart.Data)), Mode = BindingMode.OneWay, });
        source.Data = new LineChartData([new LineChartSeries("Replacement", [new(-5, 5), new(0, null), new(1, 7)])]);
        await WaitForAsync(() => numeric.Data?.Series[0].Name == "Replacement");
        var replaced = GetLinePlot(numeric).Children.OfType<ChartPath>().Single();
        Assert(
            replaced.Data is GeometryGroup { Children.Count: 2 },
            "Replacing the bound snapshot redraws two isolated points without connecting across the gap");
        source.Data = null;
        await WaitForAsync(() => numeric.Data is null);
        Assert(!GetLinePlot(numeric).Children.OfType<ChartPath>().Any(), "Clearing the bound snapshot clears the plotted series");
        numeric.ClearValue(LineChart.DataProperty);
        numeric.Data = new LineChartData(
        [
            new LineChartSeries("Forecast", [new(-2, 0), new(1, 3), new(4, 4)])
            {
                IsStrokeDashed = true, StrokeThickness = 3, ShowAreaFill = true,
            }
        ]);
        numeric.XAxis = new(0, 2, "0' units'");
        var fixedPaths = GetLinePlot(numeric).Children.OfType<ChartPath>().ToArray();
        var stroke = fixedPaths.Single(path => path.Stroke is not null);
        var strokeStart = ((PathGeometry)stroke.Data).Figures[0].StartPoint;
        Assert(
            stroke.StrokeThickness == 3
            && stroke.StrokeDashArray.Count == 2
            && stroke.Clip is RectangleGeometry clip
            && strokeStart.X < clip.Rect.Left
            && fixedPaths.Count(path => path.Fill is not null) == 1,
            "Line series style applies and fixed axis bounds clip a crossing segment at the plot edge");
        numeric.ClearValue(LineChart.XAxisProperty);
        await VerifyBoundViewportAsync(numeric);
        numeric.Data = original;

        VerifyStackedAreaGallery(controls.OfType<LineChart>().Single(chart => chart.Mode == LineChartMode.StackedArea));
    }

    private static async Task VerifyBoundViewportAsync(LineChart chart)
    {
        chart.Data = new LineChartData(
        [
            new LineChartSeries(
                "Viewport trace", Enumerable.Range(0, 201).Select(x => new LineChartPoint(x, x == 52 ? null : 2 + x % 3)).ToArray())
        ]);
        var source = new LineChart { Viewport = new(40, 60) };
        BindTo(chart, LineChart.ViewportProperty, source, nameof(LineChart.Viewport));
        var plot = GetLinePlot(chart);
        await WaitForAsync(() => plot.Children.OfType<ChartPath>().Any(path => path.Clip is RectangleGeometry));
        var stroke = plot.Children.OfType<ChartPath>().Single(path => path.Stroke is not null);
        Assert(
            stroke.Data is PathGeometry geometry
            && geometry.Figures.Count == 2
            && geometry.Figures.Sum(figure => figure.Segments.Count) < 26,
            "The viewport clips offscreen geometry and preserves a gap between visible runs");
        var visible = new ContentControl();
        var selection = new ContentControl();
        BindTo(visible, ContentControl.ContentProperty, chart, nameof(LineChart.VisibleViewport));
        BindTo(selection, ContentControl.ContentProperty, chart, nameof(LineChart.SelectedRange));
        var numericSelection = SelectRange(chart, 0.25, 0.75);
        await WaitForAsync(() => Equals(visible.Content, source.Viewport) && Equals(selection.Content, numericSelection));
        Assert(Equals(selection.Content, chart.SelectedRange), "One-way bindings observe the visible viewport and committed selection");
        Assert(
            numericSelection is { Minimum: { } min, Maximum: { } max }
            && Near(min, 45)
            && Near(max, 55)
            && chart.VisibleViewport == source.Viewport, "The selection follows the current numeric viewport");
        VerifySelectionEditing(chart);
        chart.ClearSelection();
        Assert(
            !chart.ZoomToSelection() && chart.SelectedRange is null && chart.VisibleViewport == source.Viewport,
            "Clearing a selection leaves the viewport in place");
        Assert(
            chart.PanBy(0.5) && chart.VisibleViewport?.Minimum == 50 && chart.Viewport == source.Viewport,
            "Navigation changes the visible window while preserving the requested range");
        await WaitForAsync(() => Equals(visible.Content, chart.VisibleViewport) && selection.Content is null);
        Assert(Equals(visible.Content, chart.VisibleViewport), "One-way bindings follow viewport navigation and selection clearing");
        source.Viewport = new(75, 85);
        await WaitForAsync(() => chart.VisibleViewport == source.Viewport);
        Assert(chart.VisibleViewport?.Minimum == 75, "A bound viewport source can still move the chart after a pan");
        chart.ClearValue(LineChart.ViewportProperty);
    }

    private static LineChartViewport SelectRange(LineChart chart, double first, double last)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var presenter = typeof(LineChart).GetField("_presenter", flags)!.GetValue(chart)!;
        var bounds = (Rect)presenter.GetType().GetField("_plotBounds", flags)!.GetValue(presenter)!;
        var range = (LineChartViewport)presenter.GetType().GetMethod("GetRangeBetween", flags)!.Invoke(
            presenter, [bounds.Left + bounds.Width * first, bounds.Left + bounds.Width * last])!;
        var changes = 0;
        EventHandler handler = (_, _) => changes++;
        chart.SelectionChanged += handler;
        typeof(LineChart).GetMethod("SetSelectedRange", flags)!.Invoke(chart, [range]);
        chart.SelectionChanged -= handler;
        var overlay = (Grid)presenter.GetType().GetField("_selectionOverlay", flags)!.GetValue(presenter)!;
        Assert(
            changes == 1
            && chart.SelectedRange == range
            && overlay.Visibility == Visibility.Visible
            && Near(overlay.Width, bounds.Width * Math.Abs(first - last)), "Selecting a range raises an event and draws its highlight");
        return range;
    }

    private static void VerifySelectionEditing(LineChart chart)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var presenter = typeof(LineChart).GetField("_presenter", flags)!.GetValue(chart)!;
        var presenterType = presenter.GetType();
        var plot = (Rect)presenterType.GetField("_plotBounds", flags)!.GetValue(presenter)!;
        var selection = (Rect)presenterType.GetMethod("GetSelectionBounds", flags)!.Invoke(presenter, [chart.SelectedRange])!;
        var input = presenterType.GetField("_viewportInput", flags)!.GetValue(presenter)!;
        var inputType = input.GetType();
        var editType = typeof(LineChart).Assembly.GetType("JPSoftworks.DesktopToolkit.Charts.LineChartSelectionEdit")!;
        var partType = typeof(LineChart).Assembly.GetType("JPSoftworks.DesktopToolkit.Charts.LineChartSelectionPart")!;
        var original = chart.SelectedRange!;
        Assert(
            Edit("Body", selection.Left + selection.Width / 2, plot.Right + 10) == new LineChartViewport(50, 60)
            && Edit("Body", selection.Left + selection.Width / 2, plot.Left - 10) == new LineChartViewport(40, 50),
            "Moving the range clamps to either plot boundary and preserves its span");
        var resized = Edit("StartEdge", selection.Left - 2, selection.Left - 2 + plot.Width / 8);
        Assert(
            resized.Minimum is double min
            && Math.Abs(min - 47.5) < 0.001
            && resized.Maximum == original.Maximum
            && Edit("EndEdge", selection.Right, plot.Right + 10).Minimum == original.Minimum
            && Edit("StartEdge", selection.Left, plot.Right + 10).Minimum < original.Maximum
            && chart.SelectedRange == original, "Resizing holds the opposite edge and keeps the committed selection during preview");
        var fence = (Canvas)presenterType.GetField("_selectionFence", flags)!.GetValue(presenter)!;
        Assert(
            fence.Children.OfType<Line>().Count() == 2
            && fence.Children.OfType<Line>().All(line => line.X1 == line.X2 && Near(line.Y2, plot.Height))
            && fence.Children.OfType<Rectangle>().Count(grip => grip.Width > 0 && grip.Height > 0) == 2,
            "The selection is rendered as two vertical fences with grips");

        var interaction = chart.IsViewportInteractionEnabled;
        chart.IsViewportInteractionEnabled = true;
        typeof(LineChart).GetMethod("SetSelectedRange", flags)!.Invoke(chart, [original]);
        Assert(
            CursorAt(selection.Left) == "SizeWestEast"
            && CursorAt(selection.Right) == "SizeWestEast"
            && CursorAt(selection.Left + selection.Width / 2) == "SizeAll"
            && CursorAt(plot.Left + 1) == "Cross"
            && CursorAt(plot.Left - 1) is null, "Selection cursor shows resize, move, or draw according to its hit target");
        chart.IsViewportInteractionEnabled = interaction;

        LineChartViewport Edit(string part, double anchor, double x)
        {
            var edit = Activator.CreateInstance(editType, Enum.Parse(partType, part), anchor, selection)!;
            return (LineChartViewport)inputType.GetMethod("GetEditedRange", flags)!.Invoke(input, [edit, x])!;
        }

        string? CursorAt(double x)
        {
            inputType.GetField("_hover", flags)!.SetValue(input, new Point(x, plot.Top + plot.Height / 2));
            inputType.GetMethod("RefreshCursor")!.Invoke(input, null);
            return (typeof(UIElement).GetProperty("ProtectedCursor", flags)!.GetValue(chart) as Microsoft.UI.Input.InputSystemCursor)
                ?.CursorShape.ToString();
        }
    }

    private static void VerifyLineInspectorGuide(LineChart chart, double firstX, double lastX)
    {
        Assert(
            chart.InspectionTransitionDuration == TimeSpan.FromMilliseconds(120),
            "Line chart inspection uses a short guide transition by default");
        var guide = GetLinePlot(chart).Children.OfType<Line>().Single(line => line.RenderTransform is TranslateTransform);
        var offset = (TranslateTransform)guide.RenderTransform;
        var presenter = typeof(LineChart).GetField("_presenter", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(chart)!;
        var inspector = presenter.GetType().GetField("_inspector", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(presenter)!;
        var show = inspector.GetType().GetMethod("TryShow", BindingFlags.Instance | BindingFlags.NonPublic)!;
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
        var popup = Descendants(chart).OfType<Popup>().Single(element => element.Name == "PART_Inspector");
        var animationsEnabled = new Windows.UI.ViewManagement.UISettings().AnimationsEnabled;
        var lastColumn = chart.Data!.Series.SelectMany(series => series.Points).Where(point => point.Y.HasValue).Select(point => point.X)
                             .Distinct().Count()
                         - 1;
        chart.InspectionTransitionDuration = TimeSpan.FromSeconds(10);

        Inspect(0);
        var motion = inspector.GetType().GetField("_cardMotion", fields)!.GetValue(inspector)!;
        var cardTimer = (Microsoft.UI.Dispatching.DispatcherQueueTimer)motion.GetType().GetField("_timer", fields)!.GetValue(motion)!;
        var firstCardX = CardTarget().X;
        Assert(
            popup.IsOpen && Near(Math.Abs(popup.HorizontalOffset - firstCardX), animationsEnabled ? 6 : 0),
            "The inspector opens with a short slide when animations are enabled");
        AdvanceCard(TimeSpan.FromMilliseconds(100));

        Assert(guide.Visibility == Visibility.Visible && Near(offset.X, firstX), "The first selected sample shows the guide immediately");
        Assert(
            ChartContextMenu.GetProvider(chart) is not null
            && ReadChartContext(chart, new Point(firstX + 1, guide.Y1 + 1)) is
                [{ Item: LineChartPoint, X: 0, Value: 32 }, { Item: LineChartPoint, X: 0, Value: 21 }],
            "A line context menu captures all visible samples at the targeted X");
        var lastReadout = Inspect(lastColumn);
        Assert(
            lastReadout.StartsWith("100 r/s", StringComparison.Ordinal)
            && lastReadout.Contains("110 ms", StringComparison.Ordinal)
            && lastReadout.Contains("76 ms", StringComparison.Ordinal), "Inspection reports the exact target sample while the guide moves");
        var lastCardX = CardTarget().X;
        if (animationsEnabled)
        {
            Assert(Near(offset.X, firstX), "A distant target begins at the currently displayed guide position");
            Assert(Near(popup.HorizontalOffset, firstCardX), "Moving the inspector starts at its displayed position");
            AdvanceCard(TimeSpan.FromMilliseconds(25));
            var cardInFlight = popup.HorizontalOffset;
            Assert(
                cardInFlight > Math.Min(firstCardX, lastCardX) && cardInFlight < Math.Max(firstCardX, lastCardX),
                "The inspector slides through intermediate positions");
            AdvanceGuide(TimeSpan.FromSeconds(2));
            Assert(offset.X > firstX && offset.X < lastX, "An active guide moves through intermediate plot positions");
            var inFlight = offset.X;
            Inspect(0);
            Assert(offset.X == inFlight, "Retargeting the guide continues from its in-flight position");
            Assert(popup.HorizontalOffset == cardInFlight, "Rapid retargeting preserves the inspector's in-flight position");
            AdvanceCard(TimeSpan.FromMilliseconds(100));
            Assert(
                Near(popup.HorizontalOffset, firstCardX) && !cardTimer.IsRunning,
                "The inspector lands at its new anchor and stops ticking");
            AdvanceGuide(chart.InspectionTransitionDuration);
            Assert(Near(offset.X, firstX), "The guide settles at the new target");
        }
        else
        {
            Assert(Near(offset.X, lastX), "The guide snaps when Windows animations are disabled");
            Assert(
                Near(popup.HorizontalOffset, lastCardX) && !cardTimer.IsRunning,
                "The inspector moves immediately when Windows animations are disabled");
        }

        Inspect(lastColumn);
        chart.InspectionTransitionDuration = TimeSpan.Zero;
        Assert(Near(offset.X, lastX), "A zero duration settles the guide immediately");
        Inspect(0);
        Assert(Near(offset.X, firstX), "Zero duration applies to subsequent selections");
        ChartInspection.SetIsEnabled(chart, false);
        Assert(guide.Visibility == Visibility.Collapsed, "Disabling inspection hides the guide");
        Assert(!popup.IsOpen && !cardTimer.IsRunning, "Disabling inspection cancels the card motion");
        chart.ClearValue(ChartInspection.IsEnabledProperty);
        chart.ClearValue(LineChart.InspectionTransitionDurationProperty);

        var customTemplate = CreateInspectorTemplate();
        ChartInspection.SetContentTemplate(chart, customTemplate);
        Inspect(0);
        var custom = (ContentPresenter)inspector.GetType().GetField("_customPresenter", fields)!.GetValue(inspector)!;
        Assert(
            ReferenceEquals(custom.ContentTemplate, customTemplate)
            && custom.Content is ChartInspectionContent { Rows.Count: > 0, Values: [{ Value: 32 }, { Value: 21 }] },
            "The inspector template receives typed sample values and rows");
        Inspect(lastColumn);
        Assert(
            ReferenceEquals(custom, inspector.GetType().GetField("_customPresenter", fields)!.GetValue(inspector))
            && ((ChartInspectionContent)custom.Content).Text == lastReadout, "Moving inspection refreshes the existing custom readout");
        chart.ClearValue(ChartInspection.ContentTemplateProperty);

        string Inspect(int index)
        {
            object?[] arguments = [index, null, true, false];
            if (show.Invoke(inspector, arguments) is not true)
            {
                throw new InvalidOperationException("Could not inspect the requested line chart column.");
            }

            return (string)arguments[1]!;
        }

        Point CardTarget() => (Point)motion.GetType().GetField("_target", fields)!.GetValue(motion)!;

        void AdvanceCard(TimeSpan elapsed)
        {
            var startedAt = System.Diagnostics.Stopwatch.GetTimestamp()
                            - (long)(elapsed.TotalSeconds * System.Diagnostics.Stopwatch.Frequency);
            var type = motion.GetType();
            type.GetField("_startedAt", fields)!.SetValue(motion, startedAt);
            type.GetMethod("OnTick", fields)!.Invoke(motion, [cardTimer, null]);
        }

        void AdvanceGuide(TimeSpan elapsed)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = presenter.GetType();
            var timer = type.GetField("_guideTimer", flags)!.GetValue(presenter);
            var startedAt = System.Diagnostics.Stopwatch.GetTimestamp()
                            - (long)(elapsed.TotalSeconds * System.Diagnostics.Stopwatch.Frequency);
            type.GetField("_guideStartedAt", flags)!.SetValue(presenter, startedAt);
            type.GetMethod("OnGuideTick", flags)!.Invoke(presenter, [timer, null]);
        }
    }

    private static void VerifyMonotoneGallery(LineChart chart, ToggleSwitch toggle, PathGeometry[] straight)
    {
        toggle.IsOn = true;
        var paths = GetLinePlot(chart).Children.OfType<ChartPath>().ToArray();
        var lines = paths.Where(path => path.Stroke is not null).Select(path => (PathGeometry)path.Data).ToArray();
        var fills = paths.Where(path => path.Fill is not null).Select(path => (PathGeometry)path.Data).ToArray();
        Assert(
            chart.Interpolation == LineChartInterpolation.Monotone && lines.Length == 2 && fills.Length == 2,
            "The gallery switch enables smoothing with both time series and their fills");

        var bends = false;
        for (var series = 0; series < lines.Length; series++)
        {
            var runs = lines[series].Figures;
            var fillRuns = fills[series].Figures;
            Assert(
                runs.Count == straight[series].Figures.Count && fillRuns.Count == runs.Count,
                "Smoothing preserves separate line and fill figures across gaps");
            for (var index = 0; index < runs.Count; index++)
            {
                var run = runs[index];
                var fill = fillRuns[index];
                var linear = straight[series].Figures[index];
                var samples = ((PolyLineSegment)linear.Segments[0]).Points;
                Assert(
                    !run.IsClosed
                    && run.StartPoint.Equals(linear.StartPoint)
                    && run.Segments.Count == samples.Count
                    && fill.IsClosed
                    && fill.IsFilled
                    && fill.Segments.Count == samples.Count + 2
                    && fill.Segments[0] is LineSegment opening
                    && opening.Point.Equals(run.StartPoint)
                    && fill.Segments[^1] is LineSegment closing
                    && Near(closing.Point.X, samples[^1].X),
                    "Each smoothed run starts at its first sample and its fill closes at its last sample");
                var previous = run.StartPoint;
                for (var segment = 0; segment < samples.Count; segment++)
                {
                    var stroke = (BezierSegment)run.Segments[segment];
                    var area = (BezierSegment)fill.Segments[segment + 1];
                    Assert(
                        stroke.Point3.Equals(samples[segment])
                        && area.Point3.Equals(stroke.Point3)
                        && area.Point1.Equals(stroke.Point1)
                        && area.Point2.Equals(stroke.Point2)
                        && IsMonotoneSegment(previous, stroke),
                        "The curve passes through its sample without overshoot and the fill uses the same curve");
                    bends |= !Near(stroke.Point1.Y, previous.Y + (stroke.Point3.Y - previous.Y) / 3)
                             || !Near(stroke.Point2.Y, previous.Y + (stroke.Point3.Y - previous.Y) * 2 / 3);
                    previous = stroke.Point3;
                }
            }
        }

        Assert(bends, "At least one displayed curve bends away from the straight interpolation");
        toggle.IsOn = false;
        Assert(
            chart.Interpolation == LineChartInterpolation.Linear
            && GetLinePlot(chart).Children.OfType<ChartPath>().Where(path => path.Stroke is not null).All(path =>
                ((PathGeometry)path.Data).Figures.All(run => run.Segments.Single() is PolyLineSegment)),
            "The gallery switch restores straight interpolation");
    }

    private static bool IsMonotoneSegment(Point start, BezierSegment segment)
    {
        var end = segment.Point3;
        var direction = Math.Sign(end.Y - start.Y);
        var previous = start.Y;
        for (var index = 1; index <= 16; index++)
        {
            var t = index / 16d;
            var u = 1 - t;
            var y = u * u * u * start.Y + 3 * u * u * t * segment.Point1.Y + 3 * u * t * t * segment.Point2.Y + t * t * t * end.Y;
            if (y < Math.Min(start.Y, end.Y) - 1e-8 || y > Math.Max(start.Y, end.Y) + 1e-8 || direction * (y - previous) < -1e-8)
            {
                return false;
            }

            previous = y;
        }

        return true;
    }

    private static void VerifyMonotoneNumericEdgeCases(LineChart chart)
    {
        var original = chart.Data;
        chart.Interpolation = LineChartInterpolation.Monotone;
        chart.Data = new LineChartData(
            [new LineChartSeries("Repeated X", [new(0, 0), new(10, 6), new(10, 8), new(20, 8), new(25, null), new(30, 12), new(40, 13)])]);
        var line = GetLinePlot(chart).Children.OfType<ChartPath>().Single(path => path.Stroke is not null);
        var runs = ((PathGeometry)line.Data).Figures;
        Assert(
            runs.Count == 2
            && runs[0].Segments.Count == 3
            && runs[0].Segments[0] is BezierSegment increasing
            && runs[0].Segments[1] is LineSegment vertical
            && Near(vertical.Point.X, increasing.Point3.X)
            && runs[0].Segments[2] is BezierSegment plateau
            && Near(plateau.Point3.Y, vertical.Point.Y)
            && Near(plateau.Point1.Y, plateau.Point3.Y)
            && Near(plateau.Point2.Y, plateau.Point3.Y)
            && runs[1].Segments.Single() is PolyLineSegment,
            "Numeric smoothing keeps duplicate X vertical, plateaus flat, and two-point runs straight after a gap");
        chart.Data = original;
        chart.Interpolation = LineChartInterpolation.Linear;
    }

    private static void VerifyStackedAreaGallery(LineChart chart)
    {
        var paths = GetLinePlot(chart).Children.OfType<ChartPath>().ToArray();
        var lines = paths.Where(path => path.Stroke is not null).Select(path => (PathGeometry)path.Data).ToArray();
        var fills = paths.Where(path => path.Fill is not null).Select(path => (PathGeometry)path.Data).ToArray();
        Assert(
            chart.Data?.IsTimeBased == true
            && chart.Interpolation == LineChartInterpolation.Monotone
            && lines.Length == 3
            && fills.Length == 3, "The gallery draws three stacked bands using aligned timestamp samples");
        int[] expectedRuns = [1, 2, 3];
        Assert(
            lines.Select(line => line.Figures.Count).SequenceEqual(expectedRuns)
            && fills.Select(fill => fill.Figures.Count).SequenceEqual(expectedRuns)
            && fills.All(fill => fill.Figures.All(figure => figure.IsClosed && figure.IsFilled)),
            "Missing samples split both outlines and areas of dependent bands while the lower band continues");

        var baseLine = lines[0].Figures[0];
        var baselineY = fills[0].Figures[0].Segments.OfType<LineSegment>().Single().Point.Y;
        var firstBandHeight = baselineY - baseLine.StartPoint.Y;
        var secondBandY = lines[1].Figures[0].StartPoint.Y;
        var thirdBandY = lines[2].Figures[0].StartPoint.Y;
        Assert(
            firstBandHeight > 0
            && Near(baseLine.StartPoint.Y - secondBandY, firstBandHeight * 13 / 28)
            && Near(secondBandY - thirdBandY, firstBandHeight * 8 / 28),
            "Stacked band thickness uses the original contributions on a cumulative scale");

        var upperEdge = (BezierSegment)baseLine.Segments[0];
        var adjoiningEdge = (BezierSegment)fills[1].Figures[0].Segments[^1];
        Assert(
            adjoiningEdge.Point1.Equals(upperEdge.Point2)
            && adjoiningEdge.Point2.Equals(upperEdge.Point1)
            && adjoiningEdge.Point3.Equals(baseLine.StartPoint), "Smoothed stacked bands use the same curve along their shared boundary");
        var firstGapEnd = ((BezierSegment)lines[1].Figures[0].Segments[^1]).Point3.X;
        var firstGapResume = lines[1].Figures[1].StartPoint.X;
        Assert(
            firstGapResume > firstGapEnd
            && Near(((BezierSegment)lines[2].Figures[0].Segments[^1]).Point3.X, firstGapEnd)
            && Near(lines[2].Figures[1].StartPoint.X, firstGapResume),
            "Dependent bands stop and resume at the same samples on both sides of an interior gap");

        var original = chart.Data;
        chart.Interpolation = LineChartInterpolation.Linear;
        chart.Data = new LineChartData(
        [
            new LineChartSeries("Positive", [new(0, 10), new(4, 12), new(8, 8)]),
            new LineChartSeries("Negative", [new(0, -5), new(4, -9), new(8, -13)]),
            new LineChartSeries("Changes sign", [new(0, -8), new(4, 8), new(8, 16)]),
        ]);
        paths = GetLinePlot(chart).Children.OfType<ChartPath>().ToArray();
        lines = paths.Where(path => path.Stroke is not null).Select(path => (PathGeometry)path.Data).ToArray();
        var positive = lines[0].Figures.Single();
        var negative = lines[1].Figures.Single();
        var changing = lines[2].Figures;
        var positivePoints = ((PolyLineSegment)positive.Segments.Single()).Points;
        var negativePoints = ((PolyLineSegment)negative.Segments.Single()).Points;
        var negativeEnd = ((PolyLineSegment)changing[0].Segments.Single()).Points[^1];
        var positiveStart = changing[1].StartPoint;
        Assert(
            changing.Count == 2
            && changing[0].StartPoint.Y > negative.StartPoint.Y
            && positive.StartPoint.Y < negative.StartPoint.Y
            && Near(positivePoints[0].X, (positive.StartPoint.X + positivePoints[1].X) / 2)
            && negativeEnd.Equals(negativePoints[0])
            && positiveStart.Equals(positivePoints[0]),
            "Numeric stacks separate signs and meet the correct baseline at an interpolated sign change");
        fills = paths.Where(path => path.Fill is not null).Select(path => (PathGeometry)path.Data).ToArray();
        Assert(
            fills[2].Figures.Count == 2 && fills[2].Figures.All(figure => figure.IsClosed),
            "A sign change creates separate filled bands without bridging between the two baselines");

        chart.Data = new LineChartData(
        [
            new LineChartSeries("First grid", [new(0, 1), new(2, 1)]),
            new LineChartSeries("Other grid", [new(0, 1), new(3, 1)]),
        ]);
        var details = Descendants(chart).OfType<TextBlock>().Single(label => label.Name == "PART_Details");
        Assert(
            !GetLinePlot(chart).Children.OfType<ChartPath>().Any()
            && details.Text.Contains("matching X coordinates", StringComparison.Ordinal),
            "Mismatched X coordinates show a message and draw no misleading bands");
        chart.Data = original;
        chart.Interpolation = LineChartInterpolation.Monotone;
        Assert(
            details.Text.Length == 0 && GetLinePlot(chart).Children.OfType<ChartPath>().Count() == 6,
            "Replacing mismatched data with aligned data restores the stacked chart");
    }

    private static async Task VerifyLineLegendAsync(Control[] controls)
    {
        var chart = controls.OfType<LineChart>().Single(item => item.Name == "TemperatureChart");
        var options = controls.OfType<ChartSampleOptions>().Single(panel => panel.Target == chart);
        var host = Descendants(chart).OfType<Grid>().Single(grid => grid.Name == "PART_Legend");
        var scroll = host.Children.OfType<ScrollViewer>().Single();
        var panel = (Panel)scroll.Content;
        var first = (Grid)panel.Children[0];
        var second = (Grid)panel.Children[1];
        var expected = chart.Data!.Series.Select(series => series.Points.Last(point => point.Y.HasValue).Y!.Value)
            .Select(value => value.ToString(chart.ValueFormat, CultureInfo.CurrentCulture));
        await WaitForAsync(() => Position(second).X > 0);
        Assert(
            Near(Position(second).X - first.ActualWidth, ChartLegend.GetItemSpacing(chart))
            && panel.Children.OfType<Grid>().Select(row => row.Children.OfType<TextBlock>().Last().Text).SequenceEqual(expected),
            "The temperature legend packs the entries and shows each last observed value");
        ((ComboBox)options.FindName("LegendLayoutBox")).SelectedIndex = (int)ChartLegendLayout.Vertical;
        await WaitForAsync(() => Near(Position(second).X, 0) && Position(second).Y > 0);
        Assert(ChartLegend.GetLayout(chart) == ChartLegendLayout.Vertical, "The common legend options switch the preview into a column");
        ((ComboBox)options.FindName("LegendValueBox")).SelectedIndex = (int)ChartLegendValueMode.Hidden;
        Assert(
            first.Children.OfType<TextBlock>().Last().Visibility == Visibility.Collapsed,
            "The common legend options hide values independently of the legend");
        var original = chart.Data;
        chart.Data = new LineChartData(
            Enumerable.Range(1, 12).Select(index => new LineChartSeries($"Series {index}", [new(0, index), new(1, index)])).ToArray());
        await WaitForAsync(() => scroll.ScrollableHeight > 0);
        Assert(GetLinePlot(chart).ActualHeight > scroll.ActualHeight, "A long legend scrolls without crowding out the line plot");
        chart.Data = original;
        Point Position(FrameworkElement item) => item.TransformToVisual(panel).TransformPoint(new Point());
    }

    private static Canvas GetLinePlot(LineChart chart) =>
        Descendants(chart).OfType<Canvas>().Single(canvas => canvas.Name == "PART_Plot");

    private static async Task VerifyLegendPlacementAsync(Control[] controls)
    {
        var line = controls.OfType<LineChart>().Single();
        var plot = GetLinePlot(line);
        var legend = Descendants(line).OfType<Grid>().Single(grid => grid.Name == "PART_Legend");
        var placement = FindPicker("BuiltInPlacement");
        Point LegendOffset() => legend.TransformToVisual(plot).TransformPoint(new Point());
        placement.SelectedIndex = (int)ChartLegendPlacement.End;
        await WaitForAsync(() => LegendOffset().X >= plot.ActualWidth);
        Assert(legend.ActualHeight > 0, "End placement shows the legend beside the line plot");
        placement.SelectedIndex = (int)ChartLegendPlacement.Start;
        await WaitForAsync(() => LegendOffset().X + legend.ActualWidth <= 1);
        Assert(plot.ActualWidth > 0, "Start placement moves the legend to the leading side");
        placement.SelectedIndex = (int)ChartLegendPlacement.Top;
        await WaitForAsync(() => LegendOffset().Y + legend.ActualHeight <= 1);
        Assert(legend.ActualHeight > 0, "Top placement puts the legend above the plot");
        placement.SelectedIndex = (int)ChartLegendPlacement.Bottom;
        await WaitForAsync(() => LegendOffset().Y >= plot.ActualHeight);
        Assert(legend.ActualHeight > 0, "Bottom placement puts the legend below the plot");
        var builtInItems = (Panel)legend.Children.OfType<ScrollViewer>().Single().Content;
        await WaitForAsync(() => SecondEntryOffset(builtInItems).X > 0);
        FindPicker("BuiltInLayout").SelectedIndex = (int)ChartLegendLayout.Vertical;
        await WaitForAsync(() => Near(SecondEntryOffset(builtInItems).X, 0) && SecondEntryOffset(builtInItems).Y > 0);
        Assert(
            ChartLegend.GetPlacement(line) == ChartLegendPlacement.Bottom,
            "The built-in legend switches to a column in the same placement");

        var doughnut = controls.OfType<DoughnutGraph>().Single();
        var detached = controls.OfType<ChartLegendView>().Single();
        var scroll = Descendants(detached).OfType<ScrollViewer>().Single(view => view.Name == "PART_ScrollViewer");
        await WaitForAsync(() => scroll.Content is Panel panel && panel.Children.Count == 3);
        Assert(
            GetDoughnutLegend(doughnut).Visibility == Visibility.Collapsed
            && GetLegendShares(scroll).SequenceEqual(new[] { "42 GB", "26 GB", "32 GB" }),
            "The detached view shows the chart's formatted values with its built-in legend hidden");
        var detachedItems = (Panel)scroll.Content;
        await WaitForAsync(() => Near(SecondEntryOffset(detachedItems).X, 0) && SecondEntryOffset(detachedItems).Y > 0);
        FindPicker("DetachedPlacement").SelectedIndex = 2;
        FindPicker("DetachedLayout").SelectedIndex = (int)ChartLegendLayout.Horizontal;
        await WaitForAsync(() => SecondEntryOffset(detachedItems).X > 0 && Near(SecondEntryOffset(detachedItems).Y, 0));
        Assert(
            ChartLegend.GetLayout(doughnut) == ChartLegendLayout.Horizontal,
            "The detached legend switches to wrapping rows through its source chart");
        await Task.Run(() => doughnut.SetValues(30, 45, 25));
        await WaitForAsync(() => GetLegendShares(scroll).SequenceEqual(new[] { "30 GB", "45 GB", "25 GB" }));
        Assert(true, "The detached legend reflects updates from a producer thread");

        detached.Chart = line;
        Assert(
            GetLegendShares(scroll).SequenceEqual(new[] { "72 req/s", "52 req/s", "34 req/s" }),
            "Changing the legend source replaces the entries and formatting");
        detached.Chart = doughnut;

        ComboBox FindPicker(string name) => controls.OfType<ComboBox>().Single(box => box.Name == name);

        static Point SecondEntryOffset(Panel panel) =>
            ((FrameworkElement)panel.Children[1]).TransformToVisual(panel).TransformPoint(new Point());
    }

    private static ChartContextValue[]? ReadChartContext(Control chart, Point? plotPoint)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var presenter = chart.GetType().GetField("_presenter", flags)!.GetValue(chart)!;
        var inspector = presenter.GetType().GetField("_inspector", flags)!.GetValue(presenter)!;
        var menu = inspector.GetType().GetField("_contextMenu", flags)!.GetValue(inspector)!;
        var read = (Delegate)menu.GetType().GetField("_read", flags)!.GetValue(menu)!;
        var target = read.DynamicInvoke([plotPoint]);
        return target?.GetType().GetProperty("Values")?.GetValue(target) as ChartContextValue[];
    }

    private static void VerifyBarChartGallery(Control[] controls)
    {
        var charts = controls.OfType<BarChart>().ToArray();
        var grouped = charts.Single(chart => chart.Name == "CpuChart");
        var compact = charts.Single(chart => chart.Name == "CompactCpuChart");
        VerifyGroupedBarLayout(charts.Where(chart => chart.Mode == BarChartMode.Grouped && chart != compact).ToArray());
        var threshold = GetReferenceLines(GetPlot(grouped)).Single();
        Assert(
            threshold.Clip is RectangleGeometry columnClip
            && Near(threshold.Y1, columnClip.Rect.Bottom - columnClip.Rect.Height * 0.85)
            && Near(threshold.Y1, threshold.Y2), "Column thresholds follow the numeric value axis");
        var horizontalSwitch = controls.OfType<ToggleSwitch>().Single(toggle => toggle.Name == "HorizontalSwitch");
        var labelsSwitch = controls.OfType<ToggleSwitch>().Single(toggle => toggle.Name == "HorizontalLabelsOnlySwitch");
        var mode = controls.OfType<ComboBox>().Single(box => box.Name == "ModeBox");

        bool HasCategoryLabels() => GetPlot(grouped).Children.OfType<TextBlock>()
            .Any(label => label.Text.StartsWith("CPU ", StringComparison.Ordinal));

        bool HasValueLabels() => GetPlot(grouped).Children.OfType<TextBlock>().Any(label => label.Text.EndsWith('%'));
        labelsSwitch.IsOn = true;
        Assert(HasCategoryLabels() && !HasValueLabels(), "Columns can keep only the horizontal category labels");
        horizontalSwitch.IsOn = true;
        Assert(!HasCategoryLabels() && HasValueLabels(), "Horizontal bars can keep only the horizontal value labels");
        mode.SelectedIndex = 2;
        var percentThreshold = GetReferenceLines(GetPlot(grouped)).Single();
        Assert(
            percentThreshold.Clip is RectangleGeometry barClip
            && Near(percentThreshold.X1, barClip.Rect.Left + barClip.Rect.Width * 0.85)
            && Near(percentThreshold.X1, percentThreshold.X2),
            "Horizontal 100% stacked bars project the threshold with their normalized value scale");
        mode.SelectedIndex = 0;
        labelsSwitch.IsOn = false;
        Assert(HasCategoryLabels() && HasValueLabels(), "Turning off the horizontal-only option restores both bar chart axes");
        horizontalSwitch.IsOn = false;
        var compactBars = GetChartBars(compact);
        Assert(
            ReferenceEquals(compact.Data, grouped.Data)
            && ChartAxisAppearance.GetPresentation(compact) == ChartAxisPresentation.Hidden
            && ChartLegend.GetLayout(compact) == ChartLegendLayout.Vertical
            && compactBars.Length == 12
            && compactBars.All(bar => bar.CornerRadius is { TopLeft: > 0, TopRight: > 0, BottomLeft: > 0, BottomRight: > 0 }),
            "The compact CPU comparison renders the same grouped bars with rounded corners and hidden axes");
        var signed = charts.Single(chart => chart.Mode == BarChartMode.Stacked);
        var percent = charts.Single(chart => chart.Mode == BarChartMode.Stacked100);
        var options = controls.OfType<ChartSampleOptions>().Single(panel => panel.Target == grouped);
        var palettes = (ComboBox)options.FindName("PaletteBox");
        palettes.SelectedIndex = 1;
        Assert(
            ReferenceEquals(ChartPalette.GetColors(grouped), ChartPalette.Vivid)
            && GetChartBars(grouped)[0].Background is SolidColorBrush color
            && color.Color == ChartPalette.Vivid.GetColor(0, grouped.ActualTheme),
            "The shared palette picker updates automatic bar fills on its preview");
        var signedPalette = (ComboBox)controls.OfType<ChartSampleOptions>().Single(panel => panel.Target == signed).FindName("PaletteBox");
        signedPalette.SelectedIndex = 3;
        Assert(
            ChartPalette.GetColors(grouped) == ChartPalette.Vivid && ChartPalette.GetColors(signed) == ChartPalette.Earth,
            "Gallery previews can choose their palettes independently");
        signedPalette.SelectedIndex = 0;
        var original = grouped.Data;
        var explicitBrush = new SolidColorBrush(Color.FromArgb(255, 150, 45, 75));
        grouped.Data = new BarChartData(["Category"], [new BarChartSeries("Explicit", [24], explicitBrush)]);
        palettes.SelectedIndex = 2;
        Assert(
            ReferenceEquals(GetChartBars(grouped)[0].Background, explicitBrush),
            "A series brush takes priority over a bound palette change");
        grouped.Data = original;
        palettes.SelectedIndex = 0;
        var signedToggle = controls.OfType<ToggleSwitch>().Single(toggle => toggle.Name == "SignedOrientationSwitch");
        var percentToggle = controls.OfType<ToggleSwitch>().Single(toggle => toggle.Name == "PercentOrientationSwitch");

        Assert(
            signed.Orientation == Orientation.Vertical && percent.Orientation == Orientation.Horizontal,
            "Gallery opens with a signed column stack and a horizontal 100% stack");
        VerifySignedStack(signed);
        VerifyPercentStack(percent);

        signedToggle.IsOn = true;
        percentToggle.IsOn = false;
        Assert(
            signed.Orientation == Orientation.Horizontal && percent.Orientation == Orientation.Vertical,
            "Gallery orientation switches rotate both stack examples");
        VerifySignedStack(signed);
        VerifyPercentStack(percent);
        VerifyPercentStackEdgeCases(percent);
        VerifyStackedBarStyle(controls, percent);
        signedToggle.IsOn = false;
        percentToggle.IsOn = true;
    }

    private static void VerifyGroupedBarLayout(BarChart[] charts)
    {
        Assert(charts.Length == 2, "Gallery shows two grouped bar chart examples");
        var vertical = charts.Single(chart => chart.Orientation == Orientation.Vertical);
        var horizontal = charts.Single(chart => chart.Orientation == Orientation.Horizontal);
        var columns = GetChartBars(vertical);
        Assert(
            columns.Length == 12
            && columns[0].Width > 0
            && Canvas.GetLeft(columns[0]) + columns[0].Width <= Canvas.GetLeft(columns[1])
            && Canvas.GetLeft(columns[1]) + columns[1].Width < Canvas.GetLeft(columns[2])
            && columns[1].Height > columns[0].Height
            && Near(Canvas.GetTop(columns[0]) + columns[0].Height, Canvas.GetTop(columns[1]) + columns[1].Height),
            "Vertical grouped bars sit side by side on a shared baseline");
        Assert(
            columns.All(bar => bar.CornerRadius is { TopLeft: > 0, TopRight: > 0, BottomLeft: 0, BottomRight: 0 }),
            "Vertical grouped bars round their tips and sit flush against the visible baseline");
        var bar = columns[0];
        var point = new Point(Canvas.GetLeft(bar) + bar.Width / 2, Canvas.GetTop(bar) + bar.Height / 2);
        vertical.InspectionMode = BarChartInspectionMode.Category;
        Assert(
            ChartContextMenu.GetProvider(vertical) is not null
            && ReadChartContext(vertical, point) is
            [
                { Category: "CPU 0", CategoryIndex: 0, SeriesIndex: 0, Value: 24 },
                { Category: "CPU 0", CategoryIndex: 0, SeriesIndex: 1, Value: 32 }
            ], "A bar context menu follows category inspection and captures both raw series values");
        vertical.InspectionMode = BarChartInspectionMode.Bar;

        var rows = GetChartBars(horizontal);
        Assert(
            rows.Length == 8
            && rows[0].Height > 0
            && Canvas.GetTop(rows[0]) + rows[0].Height <= Canvas.GetTop(rows[1])
            && Canvas.GetTop(rows[1]) + rows[1].Height < Canvas.GetTop(rows[2])
            && rows[0].Width > rows[1].Width
            && Near(Canvas.GetLeft(rows[4]) + rows[4].Width, Canvas.GetLeft(rows[0]))
            && AutomationProperties.GetName(rows[4]).Contains(
                (-7d).ToString(horizontal.ValueFormat, CultureInfo.CurrentCulture), StringComparison.Ordinal),
            "Horizontal grouped bars share a baseline and negative values extend in the opposite direction");
        Assert(
            rows[0].CornerRadius is { TopLeft: 0, TopRight: > 0, BottomRight: > 0, BottomLeft: 0 }
            && rows[4].CornerRadius is { TopLeft: > 0, TopRight: 0, BottomRight: 0, BottomLeft: > 0 },
            "Horizontal grouped bars keep the baseline edge square on either side of zero");

        var originalData = horizontal.Data;
        horizontal.Data = new BarChartData(["A", "B"], [new BarChartSeries("Change", [null, 4])]);
        Assert(GetChartBars(horizontal).Length == 1, "Replacing BarChart.Data updates the rendered bars and leaves gaps");
        horizontal.Data = new BarChartData(["Same", "Same"], [new BarChartSeries("Change", [2, 4]) { Key = "change" }], ["a", "b"]);
        var keyedBars = GetChartBars(horizontal);
        horizontal.Data = new BarChartData(["Same", "Same"], [new BarChartSeries("Renamed", [7, 8]) { Key = "change" }], ["b", "a"]);
        var reordered = GetChartBars(horizontal);
        Assert(
            ReferenceEquals(reordered[0], keyedBars[1])
            && ReferenceEquals(reordered[1], keyedBars[0])
            && AutomationProperties.GetName(reordered[0]).Contains("Renamed", StringComparison.Ordinal),
            "Keyed bars retain their visuals and refresh their values and labels after a reorder");
        horizontal.Data = originalData;
    }

    private static void VerifySignedStack(BarChart chart)
    {
        var segments = GetStackSpans(chart);
        var pixel = 1 / (chart.XamlRoot?.RasterizationScale ?? 1);
        Assert(
            segments.Length == 12
            && Near(segments[0].Start, segments[2].End)
            && Near(segments[0].End, segments[1].Start)
            && Near(segments[3].End, segments[2].Start)
            && segments.Take(4).All(segment => Near(segment.CategoryPosition, segments[0].CategoryPosition))
            && segments[4].CategoryPosition > segments[0].CategoryPosition
            && Math.Abs(segments[0].Length - 2 * segments[1].Length) <= 3 * pixel
            && Math.Abs(segments[2].Length - 3 * segments[3].Length) <= 4 * pixel
            && Math.Abs(segments[9].Length - segments[8].Length * 18 / 56) <= 2 * pixel
            && segments.All(segment => IsPixelAligned(segment.Start, pixel) && IsPixelAligned(segment.End, pixel)),
            $"{chart.Orientation}: positive and negative stacks start at zero and the axis fits the signed totals");
        var bars = GetChartBars(chart);
        var positiveRadius = Math.Min(4, Math.Min(bars[1].Width, bars[1].Height) / 2);
        var negativeRadius = Math.Min(4, Math.Min(bars[3].Width, bars[3].Height) / 2);
        var positiveCorners = chart.Orientation == Orientation.Horizontal
            ? new CornerRadius(0, positiveRadius, positiveRadius, 0)
            : new CornerRadius(positiveRadius, positiveRadius, 0, 0);
        var negativeCorners = chart.Orientation == Orientation.Horizontal
            ? new CornerRadius(negativeRadius, 0, 0, negativeRadius)
            : new CornerRadius(0, 0, negativeRadius, negativeRadius);
        Assert(
            bars[0].CornerRadius.Equals(new CornerRadius(0))
            && bars[2].CornerRadius.Equals(new CornerRadius(0))
            && bars[1].CornerRadius.Equals(positiveCorners)
            && bars[3].CornerRadius.Equals(negativeCorners),
            $"{chart.Orientation}: signed stacks round the outside ends and leave the zero join square");
    }

    private static void VerifyPercentStack(BarChart chart)
    {
        var segments = GetStackSpans(chart);
        var pixel = 1 / (chart.XamlRoot?.RasterizationScale ?? 1);
        var fullWidth = segments[2].End - segments[0].Start;
        Assert(
            segments.Length == 9
            && fullWidth > 0
            && Near(segments[0].End, segments[1].Start)
            && Near(segments[1].End, segments[2].Start)
            && Near(segments[0].Start, segments[3].Start)
            && Near(segments[2].End, segments[5].End)
            && Math.Abs(segments[0].Length - fullWidth * 0.6) <= pixel
            && Math.Abs(segments[3].Length - fullWidth * 0.5) <= pixel,
            $"{chart.Orientation}: 100% stacks fill the same span with category-specific shares");

        var culture = CultureInfo.CurrentCulture;
        var labels = GetPlot(chart).Children.OfType<TextBlock>().Select(label => label.Text).ToArray();
        var description = AutomationProperties.GetName(GetChartBars(chart)[0]);
        Assert(
            labels.Contains(0d.ToString("P0", culture))
            && labels.Contains(1d.ToString("P0", culture))
            && description.EndsWith($"30 ({0.6.ToString("P1", culture)})", StringComparison.Ordinal),
            $"{chart.Orientation}: 100% axis uses percentages and inspection retains raw values");
    }

    private static void VerifyPercentStackEdgeCases(BarChart chart)
    {
        var originalData = chart.Data;
        var expectedSpan = GetStackSpans(chart)[2].End - GetStackSpans(chart)[0].Start;
        chart.Minimum = -20;
        chart.Maximum = 200;
        chart.Data = new BarChartData(
            ["Large", "Empty", "Mixed"], [
                new BarChartSeries("One", [double.MaxValue, 0, -100]),
                new BarChartSeries("Two", [double.MaxValue, null, 3]),
                new BarChartSeries("Three", [null, -1, 1]),
            ]);
        var segments = GetStackSpans(chart);
        var pixel = 1 / (chart.XamlRoot?.RasterizationScale ?? 1);
        Assert(
            segments.Length == 4
            && Math.Abs(segments[0].Length - expectedSpan / 2) <= pixel
            && Math.Abs(segments[0].Length - segments[1].Length) <= pixel + 0.0001
            && Math.Abs(segments[2].Length - expectedSpan * 0.75) <= pixel
            && Math.Abs(segments[3].Length - expectedSpan * 0.25) <= pixel
            && Near(segments[3].End, segments[1].End),
            "100% stacks handle huge values, empty categories, and mixed input on a fixed percentage axis");
        chart.Minimum = double.NaN;
        chart.Maximum = double.NaN;
        chart.Data = originalData;
    }

    private static StackSpan[] GetStackSpans(BarChart chart)
    {
        return GetChartBars(chart).Select(bar => chart.Orientation == Orientation.Horizontal
            ? new StackSpan(Canvas.GetLeft(bar), Canvas.GetLeft(bar) + bar.Width, Canvas.GetTop(bar))
            : new StackSpan(-Canvas.GetTop(bar) - bar.Height, -Canvas.GetTop(bar), Canvas.GetLeft(bar))).ToArray();
    }

    private static void VerifyStackedBarStyle(Control[] controls, BarChart chart)
    {
        var gap = controls.OfType<Slider>().Single(item => item.Name == "PercentGapSlider");
        var radius = controls.OfType<Slider>().Single(item => item.Name == "PercentRadiusSlider");
        var rounding = controls.OfType<ComboBox>().Single(item => item.Name == "PercentRoundingBox");
        var original = chart.Data;
        var originalRadius = radius.Value;
        chart.Data = new BarChartData(["Mix"], [new("Main", [60]), new("Small", [2]), new("Rest", [38])]);
        var flat = GetStackSpans(chart);
        gap.Value = 12;
        radius.Value = 16;
        var rounded = GetStackSpans(chart);
        var small = GetChartBars(chart)[1];
        var pixel = 1 / (chart.XamlRoot?.RasterizationScale ?? 1);
        var smallGap = rounded[1].Start - rounded[0].End;
        Assert(
            rounded.Length == 3
            && Near(flat[0].Start, rounded[0].Start)
            && Near(flat[^1].End, rounded[^1].End)
            && smallGap > 0
            && smallGap < 12
            && small.Height >= pixel - 0.001
            && small.CornerRadius.Equals(new CornerRadius(0)),
            "Spacing a short interior segment preserves its square joins and the full stack span");

        rounding.SelectedIndex = 1;
        small = GetChartBars(chart)[1];
        Assert(
            chart.SegmentRounding == BarChartSegmentRounding.AllCorners && small.CornerRadius.Equals(new CornerRadius(small.Height / 2)),
            "Every segment rounding clamps the radius to the size of a narrow segment");
        Assert(
            GetChartBars(chart)[0].CornerRadius is { TopLeft: > 0, TopRight: > 0, BottomLeft: 0, BottomRight: 0 },
            "Every segment rounding keeps the first segment flush with the visible baseline");
        rounding.SelectedIndex = 0;
        gap.Value = 0;
        radius.Value = 0;
        Assert(GetChartBars(chart).All(bar => bar.CornerRadius.Equals(new CornerRadius(0))), "A zero radius squares all stack corners");
        radius.Value = originalRadius;
        chart.Data = new BarChartData(["Mix"], [new("Missing", [null]), new("Main", [100]), new("Empty", [0])]);
        Assert(
            GetChartBars(chart).Single().CornerRadius.Equals(new CornerRadius(originalRadius, originalRadius, 0, 0)),
            "A lone visible segment rounds its tip and keeps the visible baseline edge square");
        ChartAxisAppearance.SetPresentation(chart, ChartAxisPresentation.Hidden);
        Assert(
            GetChartBars(chart).Single().CornerRadius.Equals(new CornerRadius(originalRadius)),
            "Hiding the axes lets the lone segment round both ends");
        ChartAxisAppearance.SetPresentation(chart, ChartAxisPresentation.Standard);
        chart.ValueAxis = new() { ShowAxisLine = false };
        Assert(
            GetChartBars(chart).Single().CornerRadius.Equals(new CornerRadius(originalRadius)),
            "Disabling the baseline leaves both segment ends rounded with ticks and grid lines enabled");
        chart.ClearValue(BarChart.ValueAxisProperty);
        chart.Data = original;
    }

    private static bool IsPixelAligned(double position, double pixel) =>
        Math.Abs(position / pixel - Math.Round(position / pixel)) < 0.0001;

    private readonly record struct StackSpan(double Start, double End, double CategoryPosition)
    {
        public double Length => End - Start;
    }

    private static Canvas GetPlot(BarChart chart) =>
        Descendants(chart).OfType<Canvas>().Single(canvas => canvas.Name == "PART_Plot");

    private static Border[] GetChartBars(BarChart chart) => GetPlot(chart).Children.OfType<Border>().ToArray();

    private static void VerifyHorizontalMeterLayout(VerticalUsageBar meter)
    {
        var host = Descendants(meter).OfType<Canvas>().Single(canvas => canvas.Name == "PART_IndicatorHost");
        var first = (FrameworkElement)host.Children[0];
        var second = (FrameworkElement)host.Children[1];
        var width = host.ActualWidth;
        var height = host.ActualHeight;
        var pixel = 1 / (meter.XamlRoot?.RasterizationScale ?? 1);
        Assert(
            meter.ActualHeight < 96
            && width > 0
            && height > 0
            && Near(Canvas.GetLeft(first), 0)
            && Math.Abs(first.Width - width * 64 / 256) <= pixel
            && Near(Canvas.GetLeft(second), first.Width)
            && Math.Abs(second.Width - width * 96 / 256) <= pixel
            && Near(second.Height, height)
            && meter.Value == 160, "Gallery horizontal stacked meter preserves order, unfilled capacity, and the headline value");
        Assert(
            FillCorners(0).Equals(new CornerRadius(5, 0, 0, 5)) && FillCorners(1).Equals(new CornerRadius(0, 5, 5, 0)),
            "Horizontal stacked meter rounds the exposed ends and keeps the join square");

        meter.Orientation = Orientation.Vertical;
        first = (FrameworkElement)host.Children[0];
        second = (FrameworkElement)host.Children[1];
        Assert(
            Near(Canvas.GetTop(first) + first.Height, height)
            && Near(Canvas.GetTop(second) + second.Height, Canvas.GetTop(first))
            && Near(second.Width, width), "Changing meter orientation relays out the same stack from the bottom");
        var bottomRadius = Math.Min(5, Math.Min(first.Width, first.Height) / 2);
        var topRadius = Math.Min(5, Math.Min(second.Width, second.Height) / 2);
        Assert(
            bottomRadius > 0
            && topRadius > 0
            && FillCorners(0).Equals(new CornerRadius(0, 0, bottomRadius, bottomRadius))
            && FillCorners(1).Equals(new CornerRadius(topRadius, topRadius, 0, 0)),
            "Vertical stacked meter rounds the exposed ends and keeps the join square");
        meter.Orientation = Orientation.Horizontal;

        var duration = meter.TransitionDuration;
        meter.TransitionDuration = TimeSpan.Zero;
        meter.SetValues(0, 96);
        Assert(
            ((FrameworkElement)host.Children[0]).Visibility == Visibility.Collapsed && FillCorners(1).Equals(new CornerRadius(5)),
            "A zero contribution leaves both ends of the visible segment rounded");
        meter.SetValues(64, 96);
        meter.TransitionDuration = duration;

        CornerRadius FillCorners(int index) => ((Border)((Grid)host.Children[index]).Children[0]).CornerRadius;
    }

    private static async Task VerifyCompactSegmentedMeterAsync(Control[] controls)
    {
        var meter = controls.OfType<VerticalUsageBar>().Single(bar => bar.Name == "CompactStorageMeter");
        var queue = controls.OfType<VerticalUsageBar>().Single(bar => bar.Name == "CompactQueueMeter");
        var frame = Part<Border>(meter, "PART_TrackFrame");
        var queueFrame = Part<Border>(queue, "PART_TrackFrame");
        var inactive = Part<Border>(meter, "PART_InactiveSegment");
        var queueInactive = Part<Border>(queue, "PART_InactiveSegment");
        var standardMeter = controls.OfType<VerticalUsageBar>().Single(bar => bar.Name == "StackedBar");
        var standardBackground = ((SolidColorBrush)Part<Border>(standardMeter, "PART_TrackFrame").Background).Color;
        var compactBackground = ((SolidColorBrush)inactive.Background).Color;
        var host = Part<Canvas>(meter, "PART_IndicatorHost");
        var queueHost = Part<Canvas>(queue, "PART_IndicatorHost");
        var legend = Part<ContentControl>(meter, "PART_Legend");
        var segments = host.Children.OfType<Grid>().ToArray();
        var first = segments[0];
        var second = segments[1];
        var last = segments[^1];
        var pixel = 1 / (meter.XamlRoot?.RasterizationScale ?? 1);

        var highContrast = new Windows.UI.ViewManagement.AccessibilitySettings().HighContrast;
        var tint = meter.ActualTheme == ElementTheme.Dark ? byte.MaxValue : byte.MinValue;
        Assert(
            highContrast ? compactBackground.A == byte.MaxValue : compactBackground == Color.FromArgb(51, tint, tint, tint),
            "Unused capacity uses a twenty-percent neutral tint or an opaque system contrast brush");
        Assert(
            segments.Length == 3
            && Near(host.ActualHeight, 8)
            && Near(queueHost.ActualWidth, 8)
            && first.Children is [Border { Opacity: 1 }]
            && Part<Canvas>(meter, "PART_Grid").Children.Count == 0, "Both compact orientations render an eight-DIP solid track");
        Assert(
            frame.CornerRadius.Equals(new CornerRadius(4)) && queueFrame.CornerRadius.Equals(new CornerRadius(4)),
            "Slim compact tracks keep semicircular ends in both orientations");
        Assert(
            Near(Canvas.GetLeft(first), 0)
            && Near(Canvas.GetLeft(second) - first.Width, 2)
            && Math.Abs(Canvas.GetLeft(last) + last.Width - host.ActualWidth * 164 / 256) <= pixel
            && meter.Value == 164, "Compact gaps leave the total endpoint and headline value in place");
        var queueLast = queueHost.Children.OfType<Grid>().Last();
        Assert(
            frame.Background is SolidColorBrush { Color.A: 0 }
            && inactive.Visibility == Visibility.Visible
            && Near(Canvas.GetLeft(inactive) - Canvas.GetLeft(last) - last.Width, meter.SegmentGap)
            && Near(Canvas.GetLeft(inactive) + inactive.Width, host.ActualWidth)
            && queueInactive.Visibility == Visibility.Visible
            && Near(Canvas.GetTop(queueInactive), 0)
            && Near(Canvas.GetTop(queueLast) - queueInactive.Height, queue.SegmentGap),
            "Inactive compact capacity sits beyond a transparent gap at the end of either orientation");

        var originalQueueGap = queue.SegmentGap;
        queue.SegmentGap = 3.25 * pixel;
        var queueSegments = queueHost.Children.OfType<Grid>().Reverse().ToArray();
        var verticalGaps = MeasureVerticalGaps();
        Assert(
            verticalGaps.All(edges =>
                Math.Abs(edges.End / pixel - Math.Round(edges.End / pixel)) < 0.001
                && Math.Abs((edges.End - edges.Start) / pixel - 3) < 0.001),
            "Fractional gap requests align all vertical separator edges to physical pixels");
        var (queueBitmap, queuePixels) = await CaptureFrameAsync(queueFrame);
        Assert(verticalGaps.All(IsSharpGap), "The vertical meter renders three clear pixels per gap with crisp edges");

        var originalQueueData = queue.Data!;
        var queueDuration = queue.TransitionDuration;
        queue.TransitionDuration = TimeSpan.Zero;
        var shortContribution = 4 * pixel / queueHost.ActualHeight * (queue.Maximum - queue.Minimum);
        queue.Data = new VerticalUsageBarData(
            originalQueueData.Series.ToArray(), [originalQueueData.Values[0], shortContribution, originalQueueData.Values[2]]);
        Assert(
            queueSegments[1].Visibility == Visibility.Visible
            && MeasureVerticalGaps().All(edges => Math.Abs((edges.End - edges.Start) / pixel - 2) < 0.001),
            "A short middle segment reduces every vertical gap to the same width");
        queue.Data = originalQueueData;
        queue.TransitionDuration = queueDuration;
        queue.SegmentGap = originalQueueGap;

        var items = (Panel)legend.Content;
        Assert(
            legend.Visibility == Visibility.Visible
            && Grid.GetRow(legend) == 0
            && items.Children.Count == 3
            && Labels().Any(label => label.Text == "72 GB")
            && ReferenceEquals(
                items.Children.OfType<Grid>().First().Children.OfType<Ellipse>().Single().Fill, meter.Data!.Series[0].IndicatorBrush),
            "The built-in legend shows matching swatches and formatted contributions");
        var thickness = controls.OfType<Slider>().Single(item => item.Name == "ThicknessSlider");
        thickness.Value = 30;
        await WaitForAsync(() => Near(host.ActualHeight, 30));
        Assert(frame.CornerRadius.Equals(meter.CornerRadius), "The thick track retains its configured corner radius");
        thickness.Value = 12;
        await WaitForAsync(() => Near(host.ActualHeight, 12));
        Assert(frame.CornerRadius.Equals(new CornerRadius(6)), "Changing track thickness updates the cap radius");
        var gap = controls.OfType<Slider>().Single(item => item.Name == "GapSlider");
        gap.Value = 5;
        await WaitForAsync(() => Near(Canvas.GetLeft(second) - first.Width, 5));
        Assert(
            ReferenceEquals(first, host.Children[0]) && ReferenceEquals(second, host.Children[1]),
            "The bound gap and thickness update the existing segment visuals");

        var highlight = Part<Border>(meter, "PART_SegmentHighlight");
        var popup = Part<Popup>(meter, "PART_Inspector");
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var inspector = typeof(VerticalUsageBar).GetField("_inspector", flags)!.GetValue(meter)!;
        var inspectPointer = inspector.GetType().GetMethod("InspectPointer", flags)!;
        ChartInspection.SetIsEnabled(meter, false);
        ChartInspection.SetIsEnabled(meter, true);
        ChartInspection.SetHoverDelay(meter, TimeSpan.FromSeconds(1));
        Hover(first.Width / 2);
        Assert(
            highlight.Visibility == Visibility.Visible && !popup.IsOpen && Near(highlight.Width, first.Width),
            "Hover highlights the current segment immediately, even with an inspector delay");
        Hover(Canvas.GetLeft(second) + second.Width / 2);
        Assert(
            Near(Canvas.GetLeft(highlight), Canvas.GetLeft(second)) && Near(highlight.Width, second.Width),
            "The same highlight follows the pointer to the next segment");
        Hover(first.Width + 2);
        Assert(highlight.Visibility == Visibility.Collapsed, "A gap between segments has no highlight");
        Hover(Canvas.GetLeft(second) + second.Width / 2);
        Hover(host.ActualWidth - 1);
        Assert(highlight.Visibility == Visibility.Collapsed, "Remaining capacity has no highlight");
        meter.ClearValue(ChartInspection.HoverDelayProperty);

        controls.OfType<ComboBox>().Single(item => item.Name == "LegendPlacementBox").SelectedIndex = 1;
        var options = controls.OfType<ChartSampleOptions>().Single(panel => panel.Target == meter);
        ((ComboBox)options.FindName("LegendLayoutBox")).SelectedIndex = (int)ChartLegendLayout.Vertical;
        await WaitForAsync(() => Grid.GetRow(legend) == 2 && items.Children[1].TransformToVisual(items).TransformPoint(new Point()).Y > 0);
        Assert(
            ChartLegend.GetLayout(meter) == ChartLegendLayout.Vertical,
            "Legend layout and placement update independently of the meter direction");
        ((ComboBox)options.FindName("LegendValueBox")).SelectedIndex = (int)ChartLegendValueMode.Hidden;
        await WaitForAsync(() => ChartLegend.GetValueMode(meter) == ChartLegendValueMode.Hidden);
        Assert(
            Labels().Count(label => label.Visibility == Visibility.Visible) == 3, "Legend values can be hidden while keeping series names");
        var example = controls.OfType<ExampleCard>().Single(card => card.Name == "SegmentedExample");
        var surface = (Border)example.FindName("AlternateThemeSurface");
        var alternateTheme = meter.ActualTheme == ElementTheme.Light ? ElementTheme.Dark : ElementTheme.Light;
        example.SetPreviewTheme(alternateTheme);
        Assert(
            meter.ActualTheme == alternateTheme
            && surface.Visibility == Visibility.Visible
            && surface.Background is SolidColorBrush { Color.A: 255 }
            && Labels().All(label => ReferenceEquals(label.Foreground, meter.Foreground)),
            "The alternate preview supplies a solid themed surface and updates its legend text");
        example.SetPreviewTheme(ElementTheme.Default);
        Assert(surface.Visibility == Visibility.Collapsed, "The inherited preview uses the gallery card surface");
        ((ToggleSwitch)options.FindName("LegendSwitch")).IsOn = false;
        await WaitForAsync(() => !meter.ShowLegend);
        Assert(legend.Visibility == Visibility.Collapsed, "Hiding the legend releases its layout space");
        meter.CornerRadius = new CornerRadius(2);
        Assert(frame.CornerRadius.Equals(new CornerRadius(2)), "Compact tracks honor a smaller custom radius");
        meter.ClearValue(Control.CornerRadiusProperty);
        var customBackground = new SolidColorBrush(Microsoft.UI.Colors.Coral);
        meter.Background = customBackground;
        Assert(ReferenceEquals(inactive.Background, customBackground), "The inactive compact segment honors an explicit background");

        var originalData = meter.Data!;
        var series = originalData.Series.ToArray();
        var duration = meter.TransitionDuration;
        meter.TransitionDuration = TimeSpan.Zero;
        var used = meter.Maximum * (1 - 2 * pixel / host.ActualWidth);
        meter.Data = new VerticalUsageBarData(series, [72, 64, used - 136]);
        segments = host.Children.OfType<Grid>().ToArray();
        first = segments[0];
        second = segments[1];
        last = segments[^1];
        var gapStart = Canvas.GetLeft(first) + first.Width;
        var gapCenter = (gapStart + Canvas.GetLeft(second)) / 2;
        var firstGapPixels = (Canvas.GetLeft(second) - gapStart) / pixel;
        var secondGapPixels = (Canvas.GetLeft(last) - Canvas.GetLeft(second) - second.Width) / pixel;
        var endGapPixels = (Canvas.GetLeft(inactive) - Canvas.GetLeft(last) - last.Width) / pixel;
        Assert(
            inactive.Visibility == Visibility.Visible
            && inactive.Width >= pixel - 0.001
            && inactive.Width < 2 * pixel + 0.001
            && Math.Abs(endGapPixels - Math.Round(endGapPixels)) < 0.001
            && Math.Abs(firstGapPixels - endGapPixels) < 0.001
            && Math.Abs(secondGapPixels - endGapPixels) < 0.001
            && inactive.Width < frame.CornerRadius.TopRight
            && Near(Canvas.GetLeft(inactive) + inactive.Width, host.ActualWidth),
            "A near-full compact meter keeps at least one pixel of inactive capacity and equal whole-pixel gaps");
        var (bitmap, pixels) = await CaptureFrameAsync(frame);
        var gapColumn = (int)(gapCenter * bitmap.PixelWidth / frame.ActualWidth);
        var lastColumn = bitmap.PixelWidth - 1;
        var middleRow = bitmap.PixelHeight / 2;
        Assert(
            (endGapPixels < 0.5 || AlphaAt(gapColumn, middleRow) == 0) && AlphaAt(lastColumn, middleRow) > 0 && AlphaAt(lastColumn, 0) == 0,
            "The thin inactive segment follows the rounded cap and any remaining gap stays clear");

        meter.Data = new VerticalUsageBarData(series, [72, 64, meter.Maximum - 136]);
        Assert(inactive.Visibility == Visibility.Collapsed, "A full compact meter has no inactive segment");
        meter.Data = new VerticalUsageBarData(series, [0, 0, 0]);
        Assert(
            inactive.Visibility == Visibility.Visible && Near(Canvas.GetLeft(inactive), 0) && Near(inactive.Width, host.ActualWidth),
            "An empty compact meter uses the whole track for inactive capacity");
        meter.Data = originalData;
        meter.TransitionDuration = duration;

        meter.IsCompact = false;
        Assert(frame.CornerRadius.Equals(meter.CornerRadius), "Standard tracks use the configured corner radius");
        Assert(
            inactive.Visibility == Visibility.Collapsed && ReferenceEquals(frame.Background, customBackground),
            "Switching to standard presentation returns the explicit background to the track");
        meter.ClearValue(Control.BackgroundProperty);
        Assert(((SolidColorBrush)frame.Background).Color == standardBackground, "Clearing the background restores the standard track fill");

        var template = meter.Template;
        var replacements = (ResourceDictionary)Microsoft.UI.Xaml.Markup.XamlReader.Load(
            """
            <ResourceDictionary
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                xmlns:charts="using:JPSoftworks.DesktopToolkit.Charts">
                <ControlTemplate x:Key="TrackOnly" TargetType="charts:VerticalUsageBar">
                    <Grid x:Name="MeterRoot">
                        <Border x:Name="PART_TrackFrame">
                            <Grid x:Name="PART_Track">
                                <Canvas x:Name="PART_IndicatorHost" />
                            </Grid>
                        </Border>
                    </Grid>
                </ControlTemplate>
            </ResourceDictionary>
            """);
        meter.Template = (ControlTemplate)replacements["TrackOnly"];
        meter.IsCompact = true;
        meter.CornerRadius = new CornerRadius(16);
        meter.ApplyTemplate();
        meter.UpdateLayout();
        var replacement = Part<Border>(meter, "PART_TrackFrame");
        Assert(
            Near(replacement.ActualHeight, 12) && replacement.CornerRadius.Equals(new CornerRadius(6)),
            "A replacement track frame supports custom thickness and compact caps");
        thickness.Value = 18;
        await WaitForAsync(() => Near(replacement.ActualHeight, 18));
        var expectedRadius = replacement.ActualHeight / 2;
        Assert(
            replacement.Height == 18
            && Near(replacement.CornerRadius.TopLeft, expectedRadius)
            && Near(replacement.CornerRadius.BottomRight, expectedRadius),
            $"The replacement track responds to bound thickness changes (height {replacement.ActualHeight}, radius {replacement.CornerRadius.TopLeft})");
        thickness.Value = 12;
        meter.Template = template;
        meter.ClearValue(Control.CornerRadiusProperty);

        void Hover(double x) => inspectPointer.Invoke(inspector, [new Point(x, host.ActualHeight / 2), false]);
        IEnumerable<TextBlock> Labels() => items.Children.OfType<Grid>().SelectMany(row => row.Children.OfType<TextBlock>());
        byte AlphaAt(int x, int y) => pixels[(y * bitmap.PixelWidth + x) * 4 + 3];

        (double Start, double End)[] MeasureVerticalGaps() =>
        [
            (queueInactive.Height, Canvas.GetTop(queueSegments[0])),
            (Canvas.GetTop(queueSegments[0]) + queueSegments[0].Height, Canvas.GetTop(queueSegments[1])),
            (Canvas.GetTop(queueSegments[1]) + queueSegments[1].Height, Canvas.GetTop(queueSegments[2])),
        ];

        bool IsSharpGap((double Start, double End) edges)
        {
            var firstClearRow = (int)Math.Round(edges.Start / pixel);
            var firstSolidRow = (int)Math.Round(edges.End / pixel);
            return Enumerable.Range(firstClearRow, 3).All(row => QueueAlphaAt(row) == 0)
                   && QueueAlphaAt(firstSolidRow) == byte.MaxValue
                   && QueueAlphaAt(firstClearRow - 1) > 0
                   && QueueAlphaAt(firstClearRow - 1) == QueueAlphaAt(firstClearRow - 2);
        }

        byte QueueAlphaAt(int row) => queuePixels[(row * queueBitmap.PixelWidth + queueBitmap.PixelWidth / 2) * 4 + 3];

        static async Task<(RenderTargetBitmap Bitmap, byte[] Pixels)> CaptureFrameAsync(FrameworkElement target)
        {
            var image = new RenderTargetBitmap();
            await image.RenderAsync(target);
            var buffer = await image.GetPixelsAsync();
            var imagePixels = new byte[buffer.Length];
            using var reader = DataReader.FromBuffer(buffer);
            reader.ReadBytes(imagePixels);
            return (image, imagePixels);
        }

        static T Part<T>(Control chart, string name)
            where T : FrameworkElement =>
            Descendants(chart).OfType<T>().Single(element => element.Name == name);
    }

    private static DataTemplate CreateInspectorTemplate() =>
        (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
            """
            <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                <TextBlock Text="{Binding Text}" />
            </DataTemplate>
            """);

    private static bool Near(double first, double second) => Math.Abs(first - second) < 0.5;

    private static async Task VerifyLiveGraphLegendsAsync(Page page)
    {
        var liveLegend = (ChartLegendView)page.FindName("LiveLegend");
        var edgeLegend = (ChartLegendView)page.FindName("EdgeLegend");
        var liveGraph = (LiveAreaGraph)page.FindName("Graph");
        var edgeGraph = (LiveAreaGraph)page.FindName("EdgeGraph");
        var liveEntries = VerifySwatches(liveLegend, liveGraph);
        VerifySwatches(edgeLegend, edgeGraph);
        var reading = (TextBlock)liveEntries[0].Children[2];
        var firstValue = reading.Text;
        await WaitForAsync(() => reading.Text != firstValue);
        Assert(reading.Text.EndsWith('%'), "Live legend percentages update alongside their swatches");

        liveLegend.Width = Math.Ceiling(liveEntries.Max(entry => entry.ActualWidth)) + 8;
        await WaitForAsync(() =>
            Near(liveLegend.ActualWidth, liveLegend.Width)
            && liveEntries[1].TransformToVisual(liveEntries[0]).TransformPoint(default).Y > 1);
        Assert(liveEntries.All(entry => entry.ActualWidth <= liveLegend.Width + 1), "Narrow live legends wrap whole series entries");
        liveLegend.ClearValue(FrameworkElement.WidthProperty);

        var probe = new LiveAreaGraph { TooltipValueFormat = "0.0", TooltipValueSuffix = "%" };
        var color = Microsoft.UI.Colors.Blue;
        probe.AddSeries("Load", color, color);
        ChartLegend.SetValueMode(probe, ChartLegendValueMode.Value);
        await Task.Run(() => probe.AppendSample(25.5));
        edgeLegend.Chart = probe;
        try
        {
            var panel = GetPanel(edgeLegend);
            TextBlock Value() => (TextBlock)((Grid)panel.Children[0]).Children[2];
            Assert(Value().Text == $"{25.5:0.0}%", "Attaching a live legend reads the latest existing sample");
            var initialRow = panel.Children[0];
            await Task.Run(() => probe.AppendSample(73.5));
            await WaitForAsync(() => Value().Text == $"{73.5:0.0}%");
            Assert(ReferenceEquals(initialRow, panel.Children[0]), "A producer thread updates the existing legend row");
            probe.SeriesSource = new(new LiveGraphSeries("Load", color, color, edgeLineStyle: LiveGraphEdgeLineStyle.None));
            Assert(
                ((Grid)panel.Children[0]).Children[0] is Rectangle && Value().Text == $"{73.5:0.0}%",
                "Restyling a live series replaces its swatch and preserves its current value");
            probe.Clear();
            await WaitForAsync(() => Value().Visibility == Visibility.Collapsed);
            Assert(panel.Children.Count == 1, "Clearing live history keeps the legend key and removes the old reading");
        }
        finally
        {
            edgeLegend.Chart = edgeGraph;
        }

        static Panel GetPanel(ChartLegendView view) =>
            (Panel)Descendants(view).OfType<ScrollViewer>().Single(scroll => scroll.Name == "PART_ScrollViewer").Content;

        static Grid[] VerifySwatches(ChartLegendView view, LiveAreaGraph graph)
        {
            var series = graph.Series;
            var entries = GetPanel(view).Children.OfType<Grid>().ToArray();
            Assert(view.Chart == graph && entries.Length == series.Count, "The shared legend shows one entry per live graph series");
            for (var index = 0; index < series.Count; index++)
            {
                var item = series[index];
                var mark = entries[index].Children[0];
                var name = (TextBlock)entries[index].Children[1];
                var matches = item.EdgeLineStyle == LiveGraphEdgeLineStyle.None
                    ? mark is Rectangle { Fill: SolidColorBrush fill } && fill.Color == item.FillColor
                    : mark is Line { Stroke: SolidColorBrush stroke } line
                      && stroke.Color == item.EdgeLineColor
                      && line.StrokeThickness == item.EdgeLineThickness
                      && (line.StrokeDashArray?.Count > 0) == (item.EdgeLineStyle == LiveGraphEdgeLineStyle.Dashed);
                Assert(
                    matches && name.Text == item.Name && ((FrameworkElement)mark).ActualWidth > 0,
                    $"The {item.Name} legend swatch matches its plotted color and stroke or fill");
            }

            return entries;
        }
    }

    private static async Task VerifyInitiallyPausedGraphAsync(FrameworkElement root)
    {
        var graph = new LiveAreaGraph { Width = 300, Height = 180, BorderThickness = new Thickness(0) };
        var color = Color.FromArgb(255, 0, 120, 212);
        graph.AddSeries("Paused", color, color);
        graph.AppendSample(DateTimeOffset.UtcNow.AddSeconds(-50), 32);
        graph.AppendSample(42);
        graph.IsPaused = true;
        var draws = 0;
        var resizedDraws = 0;
        CanvasAnimatedControl? observedCanvas = null;

        void OnDraw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
        {
            Interlocked.Increment(ref draws);
            if (Math.Abs(sender.Size.Width - 420) < 1) Interlocked.Increment(ref resizedDraws);
        }

        graph.Loaded += (_, _) =>
        {
            observedCanvas = Descendants(graph).OfType<CanvasAnimatedControl>().Single();
            observedCanvas.Draw += OnDraw;
        };
        var container = (Grid)root;
        container.Children.Add(graph);
        try
        {
            // Do not call Invalidate here: loading a paused graph must draw on its own.
            await WaitForAsync(() => Volatile.Read(ref draws) > 0);
            Assert(true, "A graph paused before loading renders its initial frame");
            graph.Width = 420;
            await WaitForAsync(() => Volatile.Read(ref resizedDraws) > 0);
            Assert(true, "A paused graph redraws at its new size without explicit invalidation");

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var tooltip = Descendants(graph).OfType<Border>().Single(element => element.Name == "PART_Tooltip");
            var show = typeof(LiveAreaGraph).GetMethod("ShowInspection", flags)!;
            var rows = Descendants(graph).OfType<StackPanel>().Single(element => element.Name == "PART_TooltipValues");
            Assert(rows.Children.Count == 0, "Live inspector rows are lazy when no point has been inspected");
            Assert(ChartInspection.GetHoverDelay(graph) == TimeSpan.Zero, "The default live style keeps immediate pointer inspection");
            ChartInspection.SetHoverDelay(graph, TimeSpan.FromMilliseconds(100));
            typeof(LiveAreaGraph).GetField("_isPointerOver", flags)!.SetValue(graph, true);
            var preview = typeof(LiveAreaGraph).GetMethod("PreviewInspection", flags)!;
            preview.Invoke(graph, [new Point(50, 80)]);
            preview.Invoke(graph, [new Point(95, 80)]);
            Assert(tooltip.Visibility == Visibility.Collapsed, "The live graph waits for the configured hover delay");
            await WaitForAsync(() => tooltip.Visibility == Visibility.Visible && Near(Canvas.GetLeft(tooltip), 107));
            show.Invoke(graph, [162d, 118d]);
            var animationsEnabled = new Windows.UI.ViewManagement.UISettings().AnimationsEnabled;
            Assert(
                Near(Canvas.GetLeft(tooltip), animationsEnabled ? 107 : 174), "The live inspector uses the same fast position transition");
            await WaitForAsync(() => Near(Canvas.GetLeft(tooltip), 174));
            ChartInspection.SetIsEnabled(graph, false);
            Assert(tooltip.Visibility == Visibility.Collapsed, "Disabling live inspection hides its moving tooltip");
            ChartInspection.SetIsEnabled(graph, true);
            ChartInspection.SetContentTemplate(graph, CreateInspectorTemplate());
            Assert(rows.Children.Count == 0, "Setting a custom live inspector keeps it unloaded until inspection");
            show.Invoke(graph, [162d, 118d]);
            var presenter = rows.Children.OfType<ContentPresenter>().Single();
            Assert(
                presenter.Content is ChartInspectionContent { Rows.Count: 2, Values.Count: 1 },
                "The live inspector template receives typed values and swatches");
            show.Invoke(graph, [95d, 80d]);
            Assert(ReferenceEquals(presenter, rows.Children[0]), "Moving live inspection reuses its custom presenter");
            ChartInspection.SetIsEnabled(graph, false);
            Assert(presenter.Content is null, "Hiding live inspection releases its custom content");
        }
        finally
        {
            if (observedCanvas is not null) observedCanvas.Draw -= OnDraw;
            container.Children.Remove(graph);
        }
    }

    private static async Task VerifyCompactSparklineAsync(FrameworkElement root)
    {
        var graph = new LiveAreaGraph
        {
            Width = 196,
            Height = 56,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            BorderThickness = new Thickness(0),
            IsTooltipEnabled = false,
        };
        Assert(
            !graph.IsSparkline && graph.ShowAreaFill && !graph.ShowLatestValueMarker,
            "LiveAreaGraph defaults to the standard filled presentation");
        graph.IsSparkline = true;
        graph.ShowAreaFill = false;
        graph.ShowLatestValueMarker = true;
        var edge = Color.FromArgb(255, 0, 120, 212);
        graph.AddSeries("CPU", edge, Color.FromArgb(48, 0, 120, 212));
        graph.AppendSample(100);
        graph.IsPaused = true;

        var draws = 0;
        var drawHeight = 0d;
        CanvasAnimatedControl? canvas = null;

        void OnDraw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
        {
            Volatile.Write(ref drawHeight, sender.Size.Height);
            Interlocked.Increment(ref draws);
        }

        graph.Loaded += (_, _) =>
        {
            canvas = Descendants(graph).OfType<CanvasAnimatedControl>().Single();
            canvas.Draw += OnDraw;
        };
        var container = (Grid)root;
        container.Children.Add(graph);
        try
        {
            await WaitForAsync(() => Near(Volatile.Read(ref drawHeight), 56));
            Assert(
                Near(graph.ActualHeight, 56) && graph.GridMode == LiveGraphGridMode.Cross,
                "A paused sparkline draws at tile height and keeps the configured grid mode");

            var previous = Volatile.Read(ref draws);
            graph.ShowAreaFill = true;
            await WaitForAsync(() => Volatile.Read(ref draws) > previous);
            Assert(graph.ShowLatestValueMarker, "Changing area fill redraws a paused sparkline independently of its marker");

            previous = Volatile.Read(ref draws);
            graph.ShowLatestValueMarker = false;
            await WaitForAsync(() => Volatile.Read(ref draws) > previous);
            Assert(graph.ShowAreaFill, "Changing the marker redraws a paused sparkline independently of its area fill");

            graph.IsSparkline = false;
            graph.MinHeight = 0;
            graph.Height = 90;
            await WaitForAsync(() => Near(Volatile.Read(ref drawHeight), 90));
            Assert(Near(graph.ActualHeight, 90), "Switching to the standard graph honors an explicitly sized plot");

            graph.ClearValue(FrameworkElement.HeightProperty);
            await WaitForAsync(() => Near(Volatile.Read(ref drawHeight), 180));
            Assert(Near(graph.ActualHeight, 180), "The standard template provides a default height when no height is specified");
        }
        finally
        {
            if (canvas is not null) canvas.Draw -= OnDraw;
            container.Children.Remove(graph);
        }
    }

    private static void VerifyRadialGaugeBandContracts()
    {
        var gauge = new RadialGauge();
        Assert(
            gauge.StartAngle == 180 && gauge.SweepAngle == 180 && !gauge.IsValueLabelCentered && !gauge.IsDirectionReversed,
            "The gauge defaults to a forward upper semicircle with the standard label position");
        Throws<ArgumentOutOfRangeException>(() => gauge.StartAngle = double.NaN, "The start angle must be finite");
        Throws<ArgumentOutOfRangeException>(() => gauge.SweepAngle = 0, "The sweep must be greater than zero");
        Throws<ArgumentOutOfRangeException>(() => gauge.SweepAngle = 361, "The sweep cannot exceed a full turn");
        Assert(
            gauge.Minimum == 0 && gauge.Maximum == 1 && gauge.Value == 0 && gauge.Bands.Count == 0,
            "The radial gauge uses the inherited RangeBase defaults and starts without bands");
        var color = Color.FromArgb(255, 0, 120, 212);
        var lower = new RadialGaugeBand(-10, 0.5, color, "Lower");
        var upper = new RadialGaugeBand(0.5, 3, color, "Upper");
        var source = new[] { upper, lower };
        gauge.SetBands(source);
        source[0] = lower;
        Assert(gauge.Bands.SequenceEqual(new[] { lower, upper }), "Gauge bands are snapshotted and sorted by their lower bound");
        Throws<ArgumentOutOfRangeException>(() => new RadialGaugeBand(1, 1, color), "A gauge band requires a positive range");
        Throws<ArgumentOutOfRangeException>(() => new RadialGaugeBand(double.NaN, 1, color), "Gauge band limits must be finite");
        Throws<ArgumentException>(() => gauge.SetBands(lower, new RadialGaugeBand(0, 2, color)), "Overlapping gauge bands are rejected");
        Assert(gauge.Bands.SequenceEqual(new[] { lower, upper }), "A rejected band change leaves the last valid configuration in place");
    }

    private static async Task VerifyRadialGaugeGalleryAsync(Control[] controls)
    {
        var gauge = controls.OfType<RadialGauge>().Single();
        var slider = controls.OfType<Slider>().Single(control => control.Name == "ChargeSlider");
        var toggle = controls.OfType<ToggleSwitch>().Single(control => control.Name == "ThresholdsSwitch");
        var colorSwitch = controls.OfType<ToggleSwitch>().Single(control => control.Name == "BandColorSwitch");
        var reverseSwitch = controls.OfType<ToggleSwitch>().Single(control => control.Name == "ReverseDirectionSwitch");
        var plot = Descendants(gauge).OfType<Canvas>().Single(canvas => canvas.Name == "PART_Plot");
        var valueText = Descendants(gauge).OfType<TextBlock>().Single(label => label.Name == "PART_ValueLabel");
        var statusText = Descendants(gauge).OfType<TextBlock>().Single(label => label.Name == "PART_StatusLabel");
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(gauge)!;
        var range = (IRangeValueProvider)peer.GetPattern(PatternInterface.RangeValue)!;

        Assert(
            gauge.Value == 68
            && valueText.Text == "68%"
            && statusText.Text == "Normal"
            && peer.GetName() == "Battery charge"
            && peer.GetItemStatus() == "68%, Normal"
            && peer.GetAutomationControlType() == AutomationControlType.ProgressBar
            && range is { IsReadOnly: true, Minimum: 0, Maximum: 100, Value: 68 },
            "The battery gauge exposes its formatted value, active band and read-only range through UI Automation");
        Throws<InvalidOperationException>(() => range.SetValue(5), "UI Automation cannot change the gauge value");

        var paths = plot.Children.OfType<ChartPath>().ToArray();
        var track = ((PathGeometry)paths[0].Data).Figures.Single();
        var trackEnd = (ArcSegment)track.Segments.Single();
        Assert(
            paths.Length == 5
            && track.StartPoint.X < trackEnd.Point.X
            && Near(track.StartPoint.Y, trackEnd.Point.Y)
            && Near(trackEnd.Point.X - track.StartPoint.X, 2 * trackEnd.Size.Width)
            && trackEnd.SweepDirection == SweepDirection.Clockwise
            && !trackEnd.IsLargeArc, "The track is a semicircle with three separate threshold arcs");
        var thickness = controls.OfType<Slider>().Single(control => control.Name == "ArcThicknessSlider");
        thickness.Value = 28;
        await WaitForAsync(() => gauge.ArcThickness == 28);
        Assert(Near(plot.Children.OfType<ChartPath>().First().StrokeThickness, 28), "The gauge uses the bound thickness for its track");
        thickness.Value = 0;
        await WaitForAsync(() => gauge.ArcThickness == 0);

        slider.Value = 20;
        await WaitForAsync(() => gauge.Value == 20);
        Assert(
            valueText.Text == "20%" && statusText.Text == "Reserve" && peer.GetItemStatus() == "20%, Reserve",
            "The bound slider updates the gauge and selects the next band at a shared threshold");
        slider.Value = 50;
        await WaitForAsync(() => gauge.Value == 50);
        var midpoint = EndOf(paths[1]);
        Assert(
            Near(midpoint.X, (track.StartPoint.X + trackEnd.Point.X) / 2) && Near(midpoint.Y, track.StartPoint.Y - trackEnd.Size.Height),
            "The midpoint value reaches the top of the semicircle");
        slider.Value = 100;
        await WaitForAsync(() => gauge.Value == 100);
        Assert(
            EndOf(paths[1]).Equals(trackEnd.Point) && peer.GetItemStatus() == "100%, Normal",
            "The maximum reaches the right endpoint and includes the last threshold label");
        slider.Value = 0;
        await WaitForAsync(() => gauge.Value == 0);
        Assert(
            paths[1].Data is null && valueText.Text == "0%" && statusText.Text == "Low",
            "The minimum leaves the track empty and exposes the low threshold");

        toggle.IsOn = false;
        Assert(
            gauge.Bands.Count == 0
            && plot.Children.OfType<ChartPath>().Count() == 2
            && statusText.Visibility == Visibility.Collapsed
            && peer.GetItemStatus() == "0%", "Removing bands leaves a working gauge and a value-only accessible label");
        toggle.IsOn = true;
        slider.Value = 68;
        await WaitForAsync(() => gauge.Value == 68);

        colorSwitch.IsOn = true;
        await WaitForAsync(() => gauge.UseActiveBandColor);
        slider.Value = 20;
        await WaitForAsync(() => gauge.Value == 20);
        var highContrast = new Windows.UI.ViewManagement.AccessibilitySettings().HighContrast;
        Assert(
            highContrast
                ? paths[1].Stroke == gauge.IndicatorBrush
                : paths[1].Stroke is SolidColorBrush brush && brush.Color == gauge.Bands[1].Color,
            "The value arc uses the band at a shared threshold or the theme brush in high contrast");

        gauge.IsMirrored = true;
        var mirroredTrack = ((PathGeometry)paths[0].Data).Figures.Single();
        var mirroredEnd = (ArcSegment)mirroredTrack.Segments.Single();
        Assert(
            Near(mirroredTrack.StartPoint.X, trackEnd.Point.X)
            && Near(mirroredTrack.StartPoint.Y, trackEnd.Point.Y)
            && Near(mirroredEnd.Point.X, track.StartPoint.X)
            && Near(mirroredEnd.Point.Y, track.StartPoint.Y)
            && mirroredEnd.SweepDirection == SweepDirection.Counterclockwise
            && peer.GetItemStatus() == "20%, Reserve",
            "Mirroring reverses the gauge endpoints and arc direction while retaining the accessible value");

        toggle.IsOn = false;
        Assert(paths[1].Stroke == gauge.IndicatorBrush, "Removing the active band restores the indicator brush");
        toggle.IsOn = true;
        gauge.IsMirrored = false;
        colorSwitch.IsOn = false;
        slider.Value = 68;
        await WaitForAsync(() => gauge.Value == 68 && !gauge.IsMirrored && !gauge.UseActiveBandColor);

        var startAngle = controls.OfType<Slider>().Single(control => control.Name == "StartAngleSlider");
        var sweepAngle = controls.OfType<Slider>().Single(control => control.Name == "SweepAngleSlider");
        startAngle.Value = 90;
        sweepAngle.Value = 90;
        await WaitForAsync(() => gauge.StartAngle == 90 && gauge.SweepAngle == 90);
        var rotated = ((PathGeometry)paths[0].Data).Figures.Single();
        var rotatedEnd = (ArcSegment)rotated.Segments.Single();
        Assert(
            Near(rotated.StartPoint.X - rotatedEnd.Point.X, rotatedEnd.Size.Width)
            && Near(rotated.StartPoint.Y - rotatedEnd.Point.Y, rotatedEnd.Size.Height)
            && rotatedEnd.SweepDirection == SweepDirection.Clockwise,
            "A 90-degree start with a 90-degree sweep draws a clockwise lower-left quarter circle");

        slider.Value = 20;
        reverseSwitch.IsOn = true;
        await WaitForAsync(() => gauge.IsDirectionReversed && gauge.Value == 20);
        var reversed = ((PathGeometry)paths[1].Data).Figures.Single();
        var reversedEnd = (ArcSegment)reversed.Segments.Single();
        var expected = QuarterPoint(rotatedEnd.Size.Width, 0.8);
        var needle = plot.Children.OfType<Line>().Single();
        Assert(
            Track().StartPoint.Equals(rotated.StartPoint)
            && ((ArcSegment)Track().Segments.Single()).Point.Equals(rotatedEnd.Point)
            && reversed.StartPoint.Equals(rotatedEnd.Point)
            && reversedEnd.SweepDirection == SweepDirection.Counterclockwise
            && Near(reversedEnd.Point.X, expected.X)
            && Near(reversedEnd.Point.Y, expected.Y)
            && Near((needle.X1 + needle.X2) / 2, expected.X)
            && Near((needle.Y1 + needle.Y2) / 2, expected.Y)
            && peer.GetItemStatus() == "20%, Reserve",
            "Reversing the quarter arc swaps value endpoints and moves the needle within the same track");

        var lowBand = ((PathGeometry)plot.Children.OfType<ChartPath>().ElementAt(2).Data).Figures.Single();
        var lowBandEnd = (ArcSegment)lowBand.Segments.Single();
        var presenter = typeof(RadialGauge).GetField("_presenter", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(gauge)!;
        var hitTest = presenter.GetType().GetMethod("HitTest", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Assert(
            lowBandEnd.SweepDirection == SweepDirection.Counterclockwise
            && Equals(hitTest.Invoke(presenter, [QuarterPoint(lowBandEnd.Size.Width, 0.9)]), 1)
            && Equals(hitTest.Invoke(presenter, [QuarterPoint(lowBandEnd.Size.Width, 0.1)]), 3),
            "Reversed low and high threshold bands are inspected at their new positions");

        gauge.IsMirrored = true;
        var reflectedTrackEnd = (ArcSegment)Track().Segments.Single();
        var reflectedFill = ((PathGeometry)paths[1].Data).Figures.Single();
        Assert(
            reflectedTrackEnd.SweepDirection == SweepDirection.Counterclockwise
            && reflectedFill.StartPoint.Equals(reflectedTrackEnd.Point)
            && ((ArcSegment)reflectedFill.Segments.Single()).SweepDirection == SweepDirection.Clockwise,
            "Direction reversal works with a horizontally mirrored arc");
        gauge.IsMirrored = false;
        reverseSwitch.IsOn = false;
        await WaitForAsync(() => !gauge.IsDirectionReversed);

        sweepAngle.Value = 270;
        slider.Value = 100;
        await WaitForAsync(() => gauge.SweepAngle == 270 && gauge.Value == 100);
        var wide = ((PathGeometry)paths[0].Data).Figures.Single();
        var indicator = ((PathGeometry)paths[1].Data).Figures.Single();
        Assert(
            wide.Segments.Count == 2
            && !wide.IsClosed
            && ((ArcSegment)wide.Segments[1]).Point.Equals(((ArcSegment)indicator.Segments[1]).Point)
            && plot.Children.OfType<ChartPath>().Count() == 5,
            "A wide arc reaches the same endpoint as a full value and retains all threshold bands");

        sweepAngle.Value = 360;
        await WaitForAsync(() => gauge.SweepAngle == 360);
        var circle = ((PathGeometry)paths[0].Data).Figures.Single();
        var opposite = (ArcSegment)circle.Segments[0];
        var center = new Point((circle.StartPoint.X + opposite.Point.X) / 2, (circle.StartPoint.Y + opposite.Point.Y) / 2);
        var radius = opposite.Size.Width;
        Assert(
            circle.IsClosed
            && circle.Segments.Count == 2
            && ((ArcSegment)circle.Segments[1]).Point.Equals(circle.StartPoint)
            && center.X - radius > 0
            && center.X + radius < plot.ActualWidth
            && center.Y - radius > 0
            && center.Y + radius < plot.ActualHeight
            && valueText.Text == "100%"
            && peer.GetItemStatus() == "100%, Normal",
            "A full sweep closes inside the plot and preserves the visible and accessible readings");

        reverseSwitch.IsOn = true;
        await WaitForAsync(() => gauge.IsDirectionReversed);
        var reversedCircle = ((PathGeometry)paths[1].Data).Figures.Single();
        Assert(
            reversedCircle.IsClosed
            && reversedCircle.Segments.Count == 2
            && ((ArcSegment)reversedCircle.Segments[1]).Point.Equals(reversedCircle.StartPoint)
            && reversedCircle.Segments.Cast<ArcSegment>().All(segment => segment.SweepDirection == SweepDirection.Counterclockwise),
            "A reversed full sweep closes in the reversed direction at the maximum");
        reverseSwitch.IsOn = false;
        await WaitForAsync(() => !gauge.IsDirectionReversed);

        var centerSwitch = controls.OfType<ToggleSwitch>().Single(control => control.Name == "CenterLabelSwitch");
        var labels = (FrameworkElement)VisualTreeHelper.GetParent(valueText);
        centerSwitch.IsOn = true;
        await WaitForAsync(() => gauge.IsValueLabelCentered && Near(plot.ActualHeight, 240));
        var centeredCircle = Track();
        var halfway = ((ArcSegment)centeredCircle.Segments[0]).Point;
        Assert(
            LabelsAt(new Point((centeredCircle.StartPoint.X + halfway.X) / 2, (centeredCircle.StartPoint.Y + halfway.Y) / 2)),
            "The value and status labels share the geometric center of the full circle");

        sweepAngle.Value = 90;
        await WaitForAsync(() => gauge.SweepAngle == 90);
        var rotatedCenter = QuarterCenter();
        Assert(
            LabelsAt(rotatedCenter) && !Near(rotatedCenter.X, plot.ActualWidth / 2),
            "Centering follows the arc geometry when the circle center moves off the plot midpoint");
        gauge.IsMirrored = true;
        Assert(
            LabelsAt(QuarterCenter()) && Near(QuarterCenter().X + rotatedCenter.X, plot.ActualWidth),
            "The centered labels follow the mirrored quarter circle");
        toggle.IsOn = false;
        await WaitForAsync(() => statusText.Visibility == Visibility.Collapsed && LabelsAt(QuarterCenter()));
        Assert(peer.GetItemStatus() == "100%", "The centered value stays in place when the status label is removed");
        centerSwitch.IsOn = false;
        await WaitForAsync(() => !gauge.IsValueLabelCentered && Near(plot.ActualHeight, 186));
        Assert(!LabelsAt(QuarterCenter()), "Disabling centering restores the custom arc footer");

        PathFigure Track() => ((PathGeometry)paths[0].Data).Figures.Single();

        Point QuarterPoint(double arcRadius, double arcPosition)
        {
            var angle = (1 + arcPosition) * (Math.PI / 2);
            return new Point(rotated.StartPoint.X + arcRadius * Math.Cos(angle), rotatedEnd.Point.Y + arcRadius * Math.Sin(angle));
        }

        Point QuarterCenter()
        {
            var figure = Track();
            return new Point(figure.StartPoint.X, ((ArcSegment)figure.Segments.Single()).Point.Y);
        }

        bool LabelsAt(Point expected)
        {
            var actual = labels.TransformToVisual(plot).TransformPoint(new Point(labels.ActualWidth / 2, labels.ActualHeight / 2));
            return Near(actual.X, expected.X) && Near(actual.Y, expected.Y);
        }

        static Point EndOf(ChartPath path) =>
            ((ArcSegment)((PathGeometry)path.Data).Figures.Single().Segments.Single()).Point;
    }

    private static void VerifyFilledPieRenderer()
    {
        const int size = 96;
        var red = Color.FromArgb(255, 211, 40, 52);
        var blue = Color.FromArgb(255, 32, 108, 190);
        var green = Color.FromArgb(255, 44, 133, 92);
        var transparent = Color.FromArgb(0, 0, 0, 0);
        var series = new[]
        {
            new DoughnutGraphSeries("First", red), new DoughnutGraphSeries("Second", blue), new DoughnutGraphSeries("Third", green),
        };
        var type = typeof(DoughnutGraph).Assembly.GetType("JPSoftworks.DesktopToolkit.Charts.DoughnutSliceRenderer", throwOnError: true)!;
        using var renderer = (IDisposable)Activator.CreateInstance(type, nonPublic: true)!;
        var draw = type.GetMethod("Draw")!;
        var device = CanvasDevice.GetSharedDevice();
        using var target = new CanvasRenderTarget(device, size, size, 96);

        var pie = Render(DoughnutGraphMode.Pie, [0.5, 0.5, 0]);
        Assert(
            pie[44 * size + 56].Equals(red) && pie[44 * size + 40].Equals(blue),
            "Pie sectors fill the interior on both sides of the center");
        var ring = Render(DoughnutGraphMode.Doughnut, [0.5, 0.5, 0]);
        Assert(
            ring[44 * size + 56].A == 0 && ring[44 * size + 40].A == 0 && ring[48 * size + 86].Equals(red),
            "The same slices produce a hollow center and a colored ring in doughnut mode");
        var single = Render(DoughnutGraphMode.Pie, [1, 0, 0]);
        Assert(
            single[48 * size + 48].Equals(red) && single[44 * size + 40].Equals(red),
            "A single full pie slice covers the center and the surrounding disk");
        var contiguous = Render(DoughnutGraphMode.Pie, [0.42, 0.26, 0.32], gapAngle: 12);
        Assert(
            contiguous.SequenceEqual(Render(DoughnutGraphMode.Pie, [0.42, 0.26, 0.32])),
            "The pie is contiguous even when GapAngle is configured for the doughnut");
        Assert(InteriorIsCovered(contiguous, alpha: 255), "Opaque pie sectors cover the entire interior, including angled joins");
        var translucent = Render(DoughnutGraphMode.Pie, [0.42, 0.26, 0.32], alpha: 128);
        Assert(InteriorIsCovered(translucent, alpha: 128), "Joining pie sectors preserves their original opacity");
        var gappedRing = Render(DoughnutGraphMode.Doughnut, [0.5, 0.5, 0], gapAngle: 12);
        Assert(ring[9 * size + 48].A > 0 && gappedRing[9 * size + 48].A == 0, "The doughnut still honors its configured slice gap");

        static bool InteriorIsCovered(Color[] pixels, byte alpha)
        {
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x + 0.5 - size / 2;
                    var dy = y + 0.5 - size / 2;
                    if (dx * dx + dy * dy < 42 * 42 && Math.Abs(pixels[y * size + x].A - alpha) > 2)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        Color[] Render(DoughnutGraphMode mode, double[] values, double gapAngle = 0, byte alpha = 255)
        {
            var colors = series.Select(slice => new DoughnutGraphSeries(
                slice.Name, Color.FromArgb(alpha, slice.Color.R, slice.Color.G, slice.Color.B))).ToArray();
            using (var session = target.CreateDrawingSession())
            {
                session.Clear(transparent);
                draw.Invoke(renderer, [device, session, new Vector2(size, size), mode, colors, values, 14d, gapAngle, transparent]);
            }

            return target.GetPixelColors();
        }
    }

    private static async Task VerifyResponsiveDoughnutLegendAsync(FrameworkElement root)
    {
        var chart = new DoughnutGraph
        {
            Width = 460,
            Height = 260,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            CenterValue = "25%",
            CenterLabel = "used",
            TransitionDuration = TimeSpan.Zero,
        };
        Assert(
            chart.Mode == DoughnutGraphMode.Doughnut && !chart.ShowLegend, "Doughnut is the default presentation and the legend is opt-in");
        foreach (var name in new[] { "Apps", "Media", "System", "Archive", "Other", "Free" })
        {
            chart.AddSeries(name, Color.FromArgb(255, 0, 120, 212));
        }

        chart.SetValues(1, 3, 0, 0, 0, 0);
        chart.ShowLegend = true;
        var container = (Grid)root;
        container.Children.Add(chart);
        try
        {
            await WaitForAsync(() => chart.IsLoaded && Near(chart.ActualWidth, 460));
            var legend = GetDoughnutLegend(chart);
            var plot = Descendants(chart).OfType<Grid>().Single(grid => grid.Name == "PART_CanvasHost");
            var center = Descendants(chart).OfType<StackPanel>().Single(panel => panel.Name == "GraphCenterText");
            Point LegendOffset() => legend.TransformToVisual(plot).TransformPoint(new Point());
            Assert(
                legend.Visibility == Visibility.Visible
                && LegendOffset().X >= plot.ActualWidth
                && GetLegendShares(legend).SequenceEqual(new[] { Share(0.25), Share(0.75), Share(0), Share(0), Share(0), Share(0) }),
                "A wide chart puts the legend beside the plot with all series shares");

            chart.Mode = DoughnutGraphMode.Pie;
            Assert(
                center.Visibility == Visibility.Collapsed && legend.Visibility == Visibility.Visible,
                "Pie mode hides the center text and keeps the legend visible");
            chart.Width = 240;
            await WaitForAsync(() => Near(chart.ActualWidth, 240) && LegendOffset().Y >= plot.ActualHeight);
            var entries = (Panel)legend.Content;
            Assert(
                Near(LegendOffset().X, 0)
                && legend.ActualHeight <= 112
                && ((FrameworkElement)entries.Children[1]).TransformToVisual(entries).TransformPoint(new Point()).X > 0,
                "A narrow chart packs the legend into wrapping rows below the plot");
            ChartLegend.SetLayout(chart, ChartLegendLayout.Vertical);
            await WaitForAsync(() => legend.ScrollableHeight > 0);
            Assert(
                ChartLegend.GetLayout(chart) == ChartLegendLayout.Vertical, "A long column legend can still scroll below a narrow chart");

            chart.ShowLegend = false;
            Assert(legend.Visibility == Visibility.Collapsed, "The legend can be hidden after the chart is loaded");
            await Task.Run(() => chart.SetValues(3, 1, 0, 0, 0, 0));
            chart.ShowLegend = true;
            await WaitForAsync(() => GetLegendShares(legend)[0] == Share(0.75));
            Assert(
                GetLegendShares(legend)[1] == Share(0.25), "The legend reflects the latest producer-thread values after it is shown again");
            chart.Clear();
            Assert(GetLegendShares(legend).All(share => share == Share(0)), "Clearing the chart resets its legend shares");
            chart.Mode = DoughnutGraphMode.Doughnut;
            Assert(
                center.Visibility == Visibility.Visible && chart.CenterValue == "25%" && chart.CenterLabel == "used",
                "Returning to doughnut mode restores the configured center text");
        }
        finally
        {
            container.Children.Remove(chart);
        }
    }

    private static ScrollViewer GetDoughnutLegend(DoughnutGraph chart) =>
        Descendants(chart).OfType<ScrollViewer>().Single(view => view.Name == "PART_Legend");

    private static string[] GetLegendShares(ScrollViewer legend) =>
        ((Panel)legend.Content).Children.OfType<Grid>().Select(row => row.Children.OfType<TextBlock>().Last().Text).ToArray();

    private static string Share(double value) => value.ToString("0.#%", CultureInfo.CurrentCulture);

    private static Control[] GetSampleControls(DependencyObject page)
    {
        var descendants = Descendants(page).ToArray();
        var options = descendants.OfType<ExampleCard>().Select(card => card.Options).OfType<DependencyObject>()
            .SelectMany(content => Descendants(content).Prepend(content));
        return descendants.Concat(options).OfType<Control>().Distinct().ToArray();
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(3);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException("Win2D did not draw within three seconds.");
            await Task.Delay(50);
        }
    }

    private static void Assert(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        Checks.Add(name);
    }

    private static void Throws<T>(Action action, string name)
        where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            Checks.Add(name);
            return;
        }

        throw new InvalidOperationException(name);
    }
}
