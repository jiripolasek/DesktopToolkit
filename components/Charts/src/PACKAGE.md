# JPSoftworks Desktop Toolkit - Charts

Build WinUI 3 dashboards that show comparisons, trends, and live activity with interactive charts, gauges, and usage meters.

This package is part of the [JPSoftworks Desktop Toolkit](https://desktoptoolkit.jiripolasek.com/).

## Package Contents

The chart and indicator controls share a XAML namespace.

- `JPSoftworks.DesktopToolkit.Charts`
    - `BarChart`, `LineChart`, `ScatterChart`, `SpiderChart`, and `HeatmapChart` for comparisons and trends.
    - `SankeyChart`, `FunnelChart`, `TreemapChart`, `IcicleChart`, and `SunburstChart` for flows and hierarchies.
    - `LiveAreaGraph` and `DoughnutGraph` for changing values and proportions.
    - `VerticalUsageBar` and `RadialGauge` for meters and gauges.
    - `ChartLegendView` and `LineChartOverview` for legends and navigation.

## Documentation

See the full guide for requirements, installation, and usage examples. [Charts guide](https://desktoptoolkit.jiripolasek.com/components/charts/)

## .NET 10 trimmed publishing

If a trimmed .NET 10 publish reports `IL2081` in Windows SDK projection code,
add this property to your app's project before restore.

```xml
<PropertyGroup>
  <WindowsSdkPackageMinimumRevision>87</WindowsSdkPackageMinimumRevision>
</PropertyGroup>
```

This sets the minimum projection revision for your app's Windows target. If your SDK still selects an
older revision or your app already pins `WindowsSdkPackageVersion`, set that property to the matching
revision 87 or later, such as `10.0.19041.87` for Windows 19041. Use one projection setting per project.
Remove the workaround once your SDK selects a fixed projection by default.

## Licence

Licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).

Third-party notices are included in the package's `licenses` directory.
