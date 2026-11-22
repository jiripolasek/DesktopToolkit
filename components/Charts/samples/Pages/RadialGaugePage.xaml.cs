// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using JPSoftworks.DesktopToolkit.Charts;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Pages;

public sealed partial class RadialGaugePage : Page
{
    private readonly RadialGaugeBand[] _batteryBands =
    [
        new(0, 20, Color.FromArgb(255, 212, 68, 78), "Low"),
        new(20, 40, Color.FromArgb(255, 190, 125, 36), "Reserve"),
        new(40, 100, Color.FromArgb(255, 43, 151, 110), "Normal"),
    ];

    public RadialGaugePage()
    {
        this.InitializeComponent();
        this.BatteryExample.Xaml = """
                                   <StackPanel Spacing="12">
                                       <charts:RadialGauge x:Name="Gauge" Height="224" Maximum="100"
                                           Value="{x:Bind ChargeSlider.Value, Mode=OneWay}" BandsSource="{x:Bind BatteryBands}"
                                           StartAngle="135" SweepAngle="270" ArcThickness="18" IsValueLabelCentered="True"
                                           UseActiveBandColor="True" IsDirectionReversed="True"
                                           ValueFormat="0'%'" AutomationProperties.Name="Battery charge" />
                                       <Slider x:Name="ChargeSlider" Header="Charge (%)" Maximum="100" Value="68" />
                                   </StackPanel>
                                   """;
        this.BatteryExample.CSharp = """
                                     using JPSoftworks.DesktopToolkit.Charts;
                                     using Windows.UI;

                                     public RadialGaugeBand[] BatteryBands { get; } =
                                     [
                                         new RadialGaugeBand(0, 20, Color.FromArgb(255, 212, 68, 78), "Low"),
                                         new RadialGaugeBand(20, 40, Color.FromArgb(255, 190, 125, 36), "Reserve"),
                                         new RadialGaugeBand(40, 100, Color.FromArgb(255, 43, 151, 110), "Normal"),
                                     ];
                                     """;
    }

    private RadialGaugeBand[]? GetBatteryBands(bool showBands)
    {
        return showBands ? this._batteryBands : null;
    }
}
