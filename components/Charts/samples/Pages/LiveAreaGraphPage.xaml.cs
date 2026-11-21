// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using JPSoftworks.DesktopToolkit.Charts;
using JPSoftworks.DesktopToolkit.Controls.Demo.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Pages;

public sealed partial class LiveAreaGraphPage : Page
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private bool _initialized;
    private double _liveElapsed;
    private double _sparklineElapsed;

    public LiveGraphSeriesCollection LiveSeries { get; } = new(
        new("CPU", SamplePalette.Blue, SamplePalette.BlueFill, 2.5f),
        new("Memory", SamplePalette.Purple, SamplePalette.PurpleFill, 2f, LiveGraphEdgeLineStyle.Dashed));

    public LiveAreaGraphPage()
    {
        this.InitializeComponent();
        this.Bindings.Update();
        this.SparklineGraph.AddSeries("CPU", SamplePalette.Blue, SamplePalette.BlueFill);
        var now = DateTimeOffset.UtcNow;
        for (var halfSecond = -120; halfSecond <= 0; halfSecond++)
        {
            var second = halfSecond / 2d;
            var timestamp = now.AddSeconds(second);
            var cpu = Cpu(second);
            this.Graph.AppendSample(timestamp, cpu, Memory(second));
            this.SparklineGraph.AppendSample(timestamp, cpu);
        }

        this.LiveExample.Xaml = """
                                <charts:LiveAreaGraph x:Name="Graph" Height="280"
                                    SeriesSource="{x:Bind LiveSeries}" Interpolation="Smooth" TooltipValueFormat="0.0" TooltipValueSuffix="%"
                                    AutomationProperties.Name="CPU and memory history" />
                                """;
        this.LiveExample.CSharp = """
                                  using JPSoftworks.DesktopToolkit.Charts;
                                  using Windows.UI;

                                  public LiveGraphSeriesCollection LiveSeries { get; } = new(
                                      new("CPU", Color.FromArgb(255, 0, 120, 212),
                                          Color.FromArgb(48, 0, 120, 212), edgeLineThickness: 2.5f),
                                      new("Memory", Color.FromArgb(255, 156, 93, 216),
                                          Color.FromArgb(42, 156, 93, 216),
                                          edgeLineStyle: LiveGraphEdgeLineStyle.Dashed));

                                  // Safe from a producer thread after SeriesSource is bound.
                                  Graph.AppendSample(42, 68);
                                  """;
        this.EdgeGraph.AddSeries("Solid", SamplePalette.Blue, SamplePalette.BlueFill, 2.5f);
        this.EdgeGraph.AddSeries("Dashed", SamplePalette.Purple, SamplePalette.PurpleFill, 2f, LiveGraphEdgeLineStyle.Dashed);
        this.EdgeGraph.AddSeries(
            "Fill only", SamplePalette.Teal, Windows.UI.Color.FromArgb(60, 0, 153, 153), 2f, LiveGraphEdgeLineStyle.None);
        for (var second = -60; second <= 0; second++)
        {
            this.EdgeGraph.AppendSample(
                now.AddSeconds(second), 20 + 8 * Math.Sin(second * .12), 48 + 12 * Math.Sin(second * .16),
                78 + 10 * Math.Sin(second * .09));
        }

        this.EdgeGraph.IsPaused = true;
        this.EdgeExample.CSharp = """
                                  Graph.AddSeries("Solid", blue, blueFill,
                                      edgeLineThickness: 2.5f, edgeLineStyle: LiveGraphEdgeLineStyle.Solid);
                                  Graph.AddSeries("Dashed", purple, purpleFill,
                                      edgeLineStyle: LiveGraphEdgeLineStyle.Dashed);
                                  Graph.AddSeries("Fill only", teal, tealFill,
                                      edgeLineStyle: LiveGraphEdgeLineStyle.None);
                                  Graph.AppendSample(20, 48, 78);
                                  """;
        this.SparklineExample.Xaml = """
                                     <charts:LiveAreaGraph x:Name="Sparkline" Height="56"
                                         IsSparkline="True" Interpolation="Smooth" ShowAreaFill="True" ShowLatestValueMarker="True"
                                         Background="Transparent" BorderThickness="0"
                                         IsTooltipEnabled="False" IsTabStop="False"
                                         AutomationProperties.Name="CPU usage over the last 60 seconds" />
                                     """;
        this.SparklineExample.CSharp = """
                                       using Windows.UI;

                                       Sparkline.AddSeries("CPU", Color.FromArgb(255, 0, 120, 212),
                                           Color.FromArgb(48, 0, 120, 212));
                                       var now = DateTimeOffset.UtcNow;
                                       Sparkline.AppendSample(now.AddSeconds(-50), 28);
                                       Sparkline.AppendSample(now.AddSeconds(-30), 64);
                                       Sparkline.AppendSample(now.AddSeconds(-10), 33);
                                       Sparkline.AppendSample(now, 44);
                                       """;
        this._timer.Tick += (_, _) => this.Sample();
        this.Loaded += (_, _) => this._timer.Start();
        this.Unloaded += (_, _) => this._timer.Stop();
        this._initialized = true;
        this.UpdateValues();
        this.SparklineValue.Text = $"{Cpu(this._sparklineElapsed):0}%";
    }

    private static double Cpu(double time)
    {
        return 44 + 22 * Math.Sin(time * .19) + 9 * Math.Sin(time * .57);
    }

    private static double Memory(double time)
    {
        return 68 + 7 * Math.Sin(time * .11);
    }

    private void Sample()
    {
        var interval = this._timer.Interval.TotalSeconds;
        if (!this.Graph.IsPaused)
        {
            this._liveElapsed += interval;
            this.Graph.AppendSample(Cpu(this._liveElapsed), Memory(this._liveElapsed));
            this.UpdateValues();
        }

        this._sparklineElapsed += interval;
        var cpu = Cpu(this._sparklineElapsed);
        this.SparklineGraph.AppendSample(cpu);
        this.SparklineValue.Text = $"{cpu:0}%";
    }

    private void UpdateValues()
    {
        this.CurrentValues.Text = $"CPU {Cpu(this._liveElapsed):0.0}%  (solid)     Memory {Memory(this._liveElapsed):0.0}%  (dashed)";
    }

    private void OnPauseClick(object sender, RoutedEventArgs e)
    {
        this.Graph.IsPaused = !this.Graph.IsPaused;
        this.PauseButton.Content = this.Graph.IsPaused ? "Resume" : "Pause";
    }

    private void OnClearClick(object sender, RoutedEventArgs e)
    {
        this.Graph.Clear();
    }

    private void OnInterpolationToggled(object sender, RoutedEventArgs e)
    {
        if (this._initialized)
        {
            this.Graph.Interpolation = ((ToggleSwitch)sender).IsOn ? LiveGraphInterpolation.Smooth : LiveGraphInterpolation.Eased;
        }
    }

    private void OnSparklineFillClick(object sender, RoutedEventArgs e)
    {
        this.SparklineGraph.ShowAreaFill = this.SparklineFillBox.IsChecked == true;
    }

    private void OnSparklineMarkerClick(object sender, RoutedEventArgs e)
    {
        this.SparklineGraph.ShowLatestValueMarker = this.SparklineMarkerBox.IsChecked == true;
    }

    private void OnGridChanged(object sender, SelectionChangedEventArgs e)
    {
        if (this._initialized)
        {
            this.Graph.GridMode = (LiveGraphGridMode)this.GridModeBox.SelectedIndex;
        }
    }

    private void OnCadenceChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!this._initialized)
        {
            return;
        }

        this._timer.Stop();
        this._timer.Interval = TimeSpan.FromSeconds(this.CadenceBox.SelectedIndex switch { 1 => 1, 2 => 5, _ => .5 });
        this.Graph.TransitionDuration = this.SparklineGraph.TransitionDuration
            = TimeSpan.FromMilliseconds(Math.Max(420, this._timer.Interval.TotalMilliseconds * 0.6));
        if (this.IsLoaded)
        {
            this._timer.Start();
        }
    }
}
