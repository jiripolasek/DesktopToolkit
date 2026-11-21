// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Diagnostics;
using JPSoftworks.DesktopToolkit.Charts;
using JPSoftworks.DesktopToolkit.Controls.Demo.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Pages;

public sealed partial class SystemMonitorPage : Page
{
    private readonly SystemMetricsSampler _sampler = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly Queue<(TimeSpan ActiveTime, double Peak)> _networkPeaks = new();
    private readonly Stopwatch _activeTime = new();
    private NetworkCounterSample? _previousNetwork;
    private long? _lastNetworkRead;
    private int _samplingVersion;
    private bool _readingNetwork;

    public SystemMonitorPage()
    {
        this.InitializeComponent();
        this.History.AddSeries("CPU", SamplePalette.Blue, SamplePalette.BlueFill, 2.5f);
        this.History.AddSeries("Memory", SamplePalette.Purple, SamplePalette.PurpleFill, 2f, LiveGraphEdgeLineStyle.Dashed);
        this.NetworkHistory.AddSeries("Receive", SamplePalette.Teal, SamplePalette.TealFill, 2.5f);
        this.NetworkHistory.AddSeries("Send", SamplePalette.Purple, SamplePalette.PurpleFill, 2f, LiveGraphEdgeLineStyle.Dashed);
        this.NetworkHistory.Maximum = 64;
        this.NetworkSection.RegisterPropertyChangedCallback(Expander.IsExpandedProperty, (_, _) => this.UpdateNetworkSampling());
        this.CpuBar.IndicatorBrush = new SolidColorBrush(SamplePalette.Blue);
        this.MemoryBar.IndicatorBrush = new SolidColorBrush(SamplePalette.Purple);
        this.MemoryGraph.AddSeries("Used", SamplePalette.Purple);
        this.MemoryGraph.AddSeries("Available", SamplePalette.Slate);
        this.MonitorExample.CSharp = """
                                     // Configure series once, then update the UI from your sampler.
                                     History.AppendSample(cpuPercent, memoryPercent);
                                     CpuBar.Value = cpuPercent;
                                     MemoryBar.Value = memoryPercent;
                                     MemoryGraph.SetValues(memoryPercent, 100 - memoryPercent);
                                     MemoryGraph.CenterValue = $"{memoryPercent:0}%";

                                     // The page owns the sampler's lifetime.
                                     Loaded += (_, _) => timer.Start();
                                     Unloaded += (_, _) => timer.Stop();
                                     """;
        this._timer.Tick += (_, _) => this.TakeSample();
        this.Loaded += (_, _) =>
        {
            if (!this.History.IsPaused)
            {
                this.StartSampling();
            }
        };
        this.Unloaded += (_, _) => this.StopSampling();
    }

    private void TakeSample()
    {
        var sample = this._sampler.Read();
        this.History.AppendSample(sample.ProcessorUsage, sample.MemoryUsage);
        this.CpuBar.Value = sample.ProcessorUsage;
        this.CpuBar.ValueText = $"{sample.ProcessorUsage:0}%";
        this.MemoryBar.Value = sample.MemoryUsage;
        this.MemoryBar.ValueText = $"{sample.MemoryUsage:0}%";
        this.MemoryGraph.SetValues(sample.MemoryUsage, 100 - sample.MemoryUsage);
        this.MemoryGraph.CenterValue = $"{sample.MemoryUsage:0}%";
        AutomationProperties.SetHelpText(this.MemoryGraph, $"{sample.MemoryUsage:0}% used, {100 - sample.MemoryUsage:0}% available.");
        this.Readings.Text = $"CPU {sample.ProcessorUsage:0.0}% · Memory {sample.MemoryUsage:0.0}%";
        const double gib = 1024d * 1024d * 1024d;
        this.MemoryDetail.Text = sample.TotalPhysicalMemory == 0
            ? "Physical memory unavailable"
            : $"{sample.UsedPhysicalMemory / gib:0.0} / {sample.TotalPhysicalMemory / gib:0.0} GB used · {Environment.ProcessorCount} logical processors";
        var now = Stopwatch.GetTimestamp();
        if (!this.NetworkHistory.IsPaused
            && !this._readingNetwork
            && (this._lastNetworkRead is not long read || Stopwatch.GetElapsedTime(read, now) >= TimeSpan.FromSeconds(1)))
        {
            this._lastNetworkRead = now;
            _ = this.SampleNetworkAsync();
        }
    }

    private async Task SampleNetworkAsync()
    {
        this._readingNetwork = true;
        var version = this._samplingVersion;
        try
        {
            var sample = await Task.Run(NetworkCounterSample.Read);
            if (version != this._samplingVersion || !this.IsLoaded || this.NetworkHistory.IsPaused)
            {
                return;
            }

            var rates = sample.GetRatesSince(this._previousNetwork);
            this._previousNetwork = sample;
            if (rates is not { } traffic)
            {
                this.NetworkReadings.Text
                    = sample.Adapters.Count == 0 ? "Adapter counters unavailable" : "Waiting for comparable adapter samples";
                return;
            }

            var activeTime = this._activeTime.Elapsed;
            this._networkPeaks.Enqueue((activeTime, Math.Max(traffic.Received, traffic.Sent)));
            while (this._networkPeaks.TryPeek(out var entry) && activeTime - entry.ActiveTime > TimeSpan.FromMinutes(2))
            {
                this._networkPeaks.Dequeue();
            }

            var visibleStart = activeTime - this.NetworkHistory.HistoryDuration;
            var peak = this._networkPeaks.Where(entry => entry.ActiveTime >= visibleStart).Max(entry => entry.Peak);
            var maximum = Math.Pow(2, Math.Ceiling(Math.Log2(Math.Max(64, peak))));
            if (this.NetworkHistory.Maximum != maximum)
            {
                this.NetworkHistory.Maximum = maximum;
            }

            this.NetworkHistory.AppendSample(traffic.Received, traffic.Sent);
            this.NetworkReadings.Text = $"Receive {traffic.Received:0.##} KiB/s    Send {traffic.Sent:0.##} KiB/s";
        }
        finally
        {
            this._readingNetwork = false;
        }
    }

    private void StartSampling()
    {
        this._sampler.Read();
        this.UpdateNetworkSampling();
        this.TakeSample();
        this._timer.Start();
    }

    private void StopSampling()
    {
        this._timer.Stop();
        this.NetworkHistory.IsPaused = true;
        this.ResetNetworkBaseline();
    }

    private void ResetNetworkBaseline()
    {
        this._activeTime.Stop();
        this._samplingVersion++;
        this._previousNetwork = null;
        this._lastNetworkRead = null;
    }

    private void OnPauseClick(object sender, RoutedEventArgs e)
    {
        this.History.IsPaused = !this.History.IsPaused;
        if (this.History.IsPaused)
        {
            this.StopSampling();
        }
        else
        {
            this.StartSampling();
        }

        this.PauseButton.Content = this.History.IsPaused ? "Resume" : "Pause";
        this.SamplingStatus.Text = this.History.IsPaused ? "Sampling paused" : "Sampling every 500 ms";
    }

    private void UpdateNetworkSampling()
    {
        var enabled = this.IsLoaded && this.NetworkSection.IsExpanded && !this.History.IsPaused;
        if (this.NetworkHistory is null || enabled == !this.NetworkHistory.IsPaused)
        {
            return;
        }

        this.NetworkHistory.IsPaused = !enabled;
        if (enabled)
        {
            this._activeTime.Start();
        }
        else
        {
            this.ResetNetworkBaseline();
        }
    }

    private TimeSpan GetHistoryDuration(int selectedIndex)
    {
        return TimeSpan.FromSeconds(
            selectedIndex switch
            {
                0 => 30,
                2 => 120,
                _ => 60,
            });
    }
}
