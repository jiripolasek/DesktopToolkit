# JPSoftworks Desktop Toolkit - Date and Time Controls

Date and time controls for WinUI 3. Use CalendarDateRangePicker to choose an inclusive date range, offer presets, block unavailable dates, and add custom flyout content.

This package is part of the [JPSoftworks Desktop Toolkit](https://desktoptoolkit.jiripolasek.com/).

## Package Contents

The picker includes models for selections, presets, and blackout policies.

- `JPSoftworks.DesktopToolkit.Controls`
    - `CalendarDateRangePicker` displays the picker and commits selections.
    - `DateRange` and `DateRangeSelection` represent the chosen dates.
    - `DateRangePreset` provides a named range.
    - `DateRangeBlackoutMode` controls how blocked dates affect a selection.

## Documentation

See the full guide for requirements, installation, and usage examples. [Date and time controls guide](https://desktoptoolkit.jiripolasek.com/components/date-time/)

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
