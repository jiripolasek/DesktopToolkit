// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using JPSoftworks.DesktopToolkit.Charts;
using JPSoftworks.DesktopToolkit.Controls.Demo.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Pages;

public sealed partial class VerticalUsageBarPage : Page
{
    private bool _initialized;

    public VerticalUsageBarPage()
    {
        this.InitializeComponent();
        this.CompactStorageMeter.Data = this.ThickStorageMeter.Data = new VerticalUsageBarData(
        [
            new("Apps", new SolidColorBrush(SamplePalette.Blue)),
            new("Media", new SolidColorBrush(SamplePalette.Purple)),
            new("System", new SolidColorBrush(SamplePalette.Teal))
        ], [72, 64, 28]);
        this.CompactQueueMeter.Data = new VerticalUsageBarData(
        [
            new("Running", new SolidColorBrush(SamplePalette.Teal)),
            new("Queued", new SolidColorBrush(SamplePalette.Blue)),
            new("Blocked", new SolidColorBrush(SamplePalette.Purple))
        ], [34, 12, 6]);
        this.SegmentedExample.Xaml = """
                                     <charts:VerticalUsageBar Orientation="Horizontal" IsCompact="True"
                                         Maximum="256" Value="164" SegmentGap="2"
                                         ShowLegend="True" ValueFormat="0' GB'" Data="{x:Bind StorageData}"
                                         AutomationProperties.Name="Workspace drive capacity, slim track" />

                                     <charts:VerticalUsageBar Orientation="Horizontal" IsCompact="True" TrackThickness="30"
                                         Maximum="256" Value="164" SegmentGap="2" ValueFormat="0' GB'"
                                         Data="{x:Bind StorageData}"
                                         AutomationProperties.Name="Workspace drive capacity, thick track" />
                                     """;
        this.SegmentedExample.CSharp = """
                                       using Microsoft.UI;
                                       using Microsoft.UI.Xaml.Media;

                                       private VerticalUsageBarData StorageData { get; } = new(
                                           [new("Apps", new SolidColorBrush(Colors.DodgerBlue)),
                                            new("Media", new SolidColorBrush(Colors.MediumPurple)),
                                            new("System", new SolidColorBrush(Colors.LightSeaGreen))],
                                           [72, 64, 28]);
                                       """;
        this.SingleBar.IndicatorBrush = new SolidColorBrush(SamplePalette.Blue);
        this.CompactBar.IndicatorBrush = new SolidColorBrush(SamplePalette.Teal);
        foreach (var bar in new[] { this.StackedBar, this.HorizontalStackedBar })
        {
            bar.AddSeries("Apps", new SolidColorBrush(SamplePalette.Blue));
            bar.AddSeries("Media", new SolidColorBrush(SamplePalette.Purple));
        }

        this.SingleExample.Xaml = """
                                  <charts:VerticalUsageBar Width="72" Height="220"
                                      Minimum="0" Maximum="100" Value="42" ValueText="42%"
                                      AutomationProperties.Name="Resource usage" />
                                  """;
        this.SingleExample.CSharp = """
                                    Meter.Value = 42;
                                    Meter.ValueText = "42%";
                                    Meter.ShowGrid = true;
                                    Meter.TransitionDuration = TimeSpan.FromMilliseconds(420);
                                    // Null or empty ValueText removes the footer.
                                    """;
        this.StackedExample.Xaml = """
                                   <charts:VerticalUsageBar x:Name="Meter" Height="54" Orientation="Horizontal"
                                       Minimum="0" Maximum="256"
                                       ShowGrid="False"
                                       AutomationProperties.Name="Storage used by apps and media" />
                                   """;
        this.StackedExample.CSharp = """
                                     using Microsoft.UI.Xaml.Media;
                                     using Windows.UI;

                                     Meter.AddSeries("Apps", new SolidColorBrush(Color.FromArgb(255, 0, 120, 212)));
                                     Meter.AddSeries("Media", new SolidColorBrush(Color.FromArgb(255, 156, 93, 216)));
                                     Meter.SetValues(64, 96); // 160 GB used; the remaining 96 GB stays empty.

                                     // SetValues does not change the headline value or its accessible range.
                                     Meter.Value = 64 + 96;
                                     Meter.ValueText = "160 GB";
                                     """;
        this._initialized = true;
        this.UpdateValues();
        this.UpdateStackedValues();
    }

    private void OnUsageChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (this._initialized)
        {
            this.UpdateValues();
        }
    }

    private UsageBarLegendPlacement GetLegendPlacement(int index)
    {
        return index == 0 ? UsageBarLegendPlacement.Start : UsageBarLegendPlacement.End;
    }

    private void UpdateValues()
    {
        var value = this.UsageSlider.Value;
        this.SingleBar.Value = this.CompactBar.Value = value;
        this.SingleBar.ValueText = $"{value:0}%";
    }

    private void OnStackedUsageChanged(object sender, RangeBaseValueChangedEventArgs args)
    {
        if (this._initialized)
        {
            this.UpdateStackedValues();
        }
    }

    private void UpdateStackedValues()
    {
        var apps = this.AppsSlider.Value;
        var media = this.MediaSlider.Value;
        var capacity = this.StackedBar.Maximum;
        var used = apps + media;

        // Each category can use the space left by the other without changing its value.
        this.AppsSlider.Maximum = capacity - media;
        this.MediaSlider.Maximum = capacity - apps;
        foreach (var bar in new[] { this.StackedBar, this.HorizontalStackedBar })
        {
            bar.Value = used;
            bar.ValueText = $"{used:0} GB";
            bar.SetValues(apps, media);
            AutomationProperties.SetHelpText(bar, $"{used:0} of {capacity:0} GB used. Apps: {apps:0} GB. Media: {media:0} GB.");
        }

        this.AppsLegend.Text = $"Apps · {apps:0} GB";
        this.MediaLegend.Text = $"Media · {media:0} GB";
        this.FreeSpaceLegend.Text = $"Free · {capacity - used:0} GB";
    }

    private void OnOptionsClick(object sender, RoutedEventArgs e)
    {
        foreach (var bar in new[] { this.SingleBar, this.CompactBar })
        {
            bar.ShowGrid = this.GridBox.IsChecked == true;
            bar.TransitionDuration = this.AnimationBox.IsChecked == true ? TimeSpan.FromMilliseconds(420) : TimeSpan.Zero;
        }
    }
}
