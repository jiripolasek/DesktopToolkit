// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using JPSoftworks.DesktopToolkit.Charts;
using JPSoftworks.DesktopToolkit.Controls.Demo.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Pages;

public sealed partial class MemoryAnalysisPage : Page
{
    private MemoryFrame[] _frames = [];
    private CancellationTokenSource? _capture;
    private HierarchyChartData? _payload;
    private bool _ready;
    private bool _captured;

    public MemoryAnalysisPage()
    {
        this.InitializeComponent();
        ChartContextMenu.SetProvider(this.HeapHistory, ChartDetailsMenuProvider.Instance);
        ChartContextMenu.SetProvider(this.BufferHistory, ChartDetailsMenuProvider.Instance);
        ChartContextMenu.SetProvider(this.HeapDelta, ChartDetailsMenuProvider.Instance);
        ChartContextMenu.SetProvider(this.RetainedPayload, ChartDetailsMenuProvider.Instance);
        this.TimelineExample.Xaml = """
                                    <!-- Page resource -->
                                    <charts:LineChartViewportGroup x:Key="MemoryTimeline" IsSelectionSynchronized="True" />

                                    <charts:LineChart x:Name="HeapHistory" Height="215" Mode="StackedArea"
                                        ViewportGroup="{StaticResource MemoryTimeline}" IsViewportInteractionEnabled="True" IsZoomEnabled="True"
                                        XValueFormat="HH:mm:ss" ValueFormat="0.#' MiB'" />
                                    <charts:LineChart x:Name="BufferHistory" Height="130" ShowAreaFill="True" ShowLegend="False"
                                        ViewportGroup="{StaticResource MemoryTimeline}" IsViewportInteractionEnabled="True" IsZoomEnabled="True"
                                        XValueFormat="HH:mm:ss" ValueFormat="0.#' MiB'" />
                                    <charts:LineChartOverview Target="{x:Bind HeapHistory}" />
                                    """;
        this.TimelineExample.CSharp = """
                                      // After InitializeComponent.
                                      var start = DateTimeOffset.UtcNow;
                                      var end = start.AddSeconds(10);
                                      HeapHistory.Data = new LineChartData(
                                      [
                                          new("Gen 0", [new(start, 5), new(end, 3)]),
                                          new("Gen 1", [new(start, 8), new(end, 10)]),
                                          new("Gen 2", [new(start, 12), new(end, 18)]),
                                      ]);
                                      BufferHistory.Data = new LineChartData(
                                          [new("Workload buffers", [new(start, 6), new(end, 13)])]);
                                      """;
        this.DeltaExample.Xaml = """
                                 <charts:BarChart x:Name="HeapDelta" Height="250" Orientation="Horizontal" ShowLegend="False"
                                     ValueFormat="+0.##' MiB';-0.##' MiB';0' MiB'" />
                                 """;
        this.DeltaExample.CSharp = """
                                   // Changes between the start and end of the selected interval.
                                   HeapDelta.Data = new BarChartData(
                                       ["Gen 0", "Gen 1", "Gen 2"], [new("Heap change", [-2, 2, 6])]);
                                   """;
        this.PayloadExample.Xaml = """
                                   <charts:SunburstChart x:Name="RetainedPayload" Height="290" MaxDepth="2" ValueFormat="0.#' MiB'" />
                                   """;
        this.PayloadExample.CSharp = """
                                     RetainedPayload.Data = new HierarchyChartData(
                                         new HierarchyChartNode("buffers", "Workload buffers",
                                             new HierarchyChartNode("images", "Image cache",
                                                 new HierarchyChartNode("thumbs", "Thumbnails", 6),
                                                 new HierarchyChartNode("previews", "Previews", 4)),
                                             new HierarchyChartNode("index", "Search index",
                                                 new HierarchyChartNode("metadata", "Photo metadata", 3))));
                                     """;
        this.Unloaded += (_, _) => this._capture?.Cancel();
        this._ready = true;
        this.LoadReplay();
    }

    private void OnIntervalChanged(object sender, SelectionChangedEventArgs args)
    {
        if (this._ready)
        {
            this.UpdateInterval();
        }
    }

    private async void OnCaptureClick(object sender, RoutedEventArgs args)
    {
        if (this._capture is not null)
        {
            return;
        }

        using var capture = new CancellationTokenSource();
        this._capture = capture;
        this.CaptureButton.IsEnabled = false;
        this.CaptureStatus.Text = "Capturing managed heap sizes after each collection...";
        try
        {
            var frames = await Task.Run(() => MemoryAnalysisSample.CaptureAsync(capture.Token), capture.Token);
            if (capture.IsCancellationRequested)
            {
                return;
            }

            this.SetTimeline(frames, captured: true);
            this.CaptureStatus.Text = "Captured in this gallery process. Buffer sizes include only this workload's payload.";
        }
        catch (OperationCanceledException) when (capture.IsCancellationRequested)
        {
            this.CaptureStatus.Text = this._captured ? "Last captured workload" : "Example replay";
        }
        finally
        {
            if (ReferenceEquals(this._capture, capture))
            {
                this._capture = null;
                this.CaptureButton.IsEnabled = true;
            }
        }
    }

    private void OnReplayClick(object sender, RoutedEventArgs args)
    {
        this._capture?.Cancel();
        this.LoadReplay();
    }

    private void LoadReplay()
    {
        this.SetTimeline(MemoryAnalysisSample.Replay(), captured: false);
        this.CaptureStatus.Text = "Example replay. Capture a workload to measure this gallery process.";
    }

    private void SetTimeline(MemoryFrame[] frames, bool captured)
    {
        this._frames = frames;
        this._captured = captured;
        this.HeapHistory.XValueFormat = captured ? "HH:mm:ss.fff" : "HH:mm:ss";
        this.BufferHistory.XValueFormat = this.HeapHistory.XValueFormat;
        this.HeapHistory.Data = new LineChartData(
            MemoryAnalysisSample.Generations.Select((name, generation) => new LineChartSeries(
                name, frames.Select(frame => new LineChartPoint(frame.Timestamp, frame.HeapSizes[generation])).ToArray())).ToArray());
        this.BufferHistory.Data = new LineChartData(
        [
            new(
                "Workload buffers",
                frames.Select(frame => new LineChartPoint(frame.Timestamp, frame.Thumbnails + frame.Previews + frame.Metadata)).ToArray())
        ]);
        this.UpdateInterval();
    }

    private void UpdateInterval()
    {
        var (start, end) = this.IntervalBox.SelectedIndex switch
        {
            1 => this._captured ? (1, 7) : (7, 29),
            2 => this._captured ? (7, 12) : (29, 48),
            _ => (0, this._frames.Length - 1),
        };
        var first = this._frames[start];
        var last = this._frames[end];
        this.HeapHistory.Viewport
            = start == 0 && end == this._frames.Length - 1 ? null : new LineChartViewport(first.Timestamp, last.Timestamp);
        this.UpdateTimelineControls();
        var deltas = new double?[MemoryAnalysisSample.Generations.Count];
        for (var generation = 0; generation < deltas.Length; generation++)
        {
            deltas[generation] = last.HeapSizes[generation] - first.HeapSizes[generation];
        }

        this.HeapDelta.Data = new BarChartData(MemoryAnalysisSample.Generations.ToArray(), [new("Heap change", deltas)]);
        this.IntervalSummary.Text
            = $"Selected interval: {first.HeapTotal:0.#} MiB to {last.HeapTotal:0.#} MiB over {(last.Timestamp - first.Timestamp).TotalSeconds:0.#} s.";

        this._payload = new HierarchyChartData(
            new HierarchyChartNode(
                "buffers", "Workload buffers",
                new HierarchyChartNode(
                    "images", "Image cache", new HierarchyChartNode("thumbs", "Thumbnails", last.Thumbnails),
                    new HierarchyChartNode("previews", "Previews", last.Previews)),
                new HierarchyChartNode("index", "Search index", new HierarchyChartNode("metadata", "Photo metadata", last.Metadata))));
        this.RetainedPayload.Data = this._payload;
        this.ResetBuffers.IsEnabled = false;
    }

    private void OnTimelineViewportChanged(object? sender, EventArgs args)
    {
        this.UpdateTimelineControls();
    }

    private void OnTimelineSelectionChanged(object? sender, EventArgs args)
    {
        this.SelectionHint.Text = this.HeapHistory.SelectedRange is null && this.BufferHistory.SelectedRange is null
            ? "Drag either plot to select a range. Zoom with Ctrl+wheel or pinch."
            : "Both plots share the range. Drag inside to move it, or drag a handle to resize it. Click inside or press Enter to zoom.";
    }

    private void OnZoomInClick(object sender, RoutedEventArgs args)
    {
        this.HeapHistory.ZoomBy(1.5);
    }

    private void OnZoomOutClick(object sender, RoutedEventArgs args)
    {
        this.HeapHistory.ZoomBy(1 / 1.5);
    }

    private void OnFitTimelineClick(object sender, RoutedEventArgs args)
    {
        this.HeapHistory.ResetViewport();
    }

    private void UpdateTimelineControls()
    {
        if (!this._ready || this.HeapHistory.Data?.Extent is not { Start: { } start, End: { } end })
        {
            return;
        }

        var first = this.HeapHistory.VisibleViewport?.Start ?? start;
        var last = this.HeapHistory.VisibleViewport?.End ?? end;
        var duration = last - first;
        var zoomed = duration < end - start;
        this.ZoomOutButton.IsEnabled = zoomed;
        this.FitTimelineButton.IsEnabled = zoomed;
        this.VisibleDuration.Text = $"Visible {duration.TotalSeconds:0.###} s";
    }

    private void OnPayloadInvoked(object? sender, HierarchyNodeInvokedEventArgs args)
    {
        if (args.Node.Children.Count > 0 && args.Node != this.RetainedPayload.Data?.Root)
        {
            this.RetainedPayload.Data = new HierarchyChartData(args.Node);
            this.ResetBuffers.IsEnabled = true;
            args.Handled = true;
        }
    }

    private void OnResetBuffersClick(object sender, RoutedEventArgs args)
    {
        this.RetainedPayload.Data = this._payload;
        this.ResetBuffers.IsEnabled = false;
        this.RetainedPayload.Focus(FocusState.Programmatic);
    }
}
