# JPSoftworks Desktop Toolkit - WinUI Styles

Give a WinUI 3 search box a pill shape and a leading magnifier using an opt-in style for `AutoSuggestBox`.

This package is part of the [JPSoftworks Desktop Toolkit](https://desktoptoolkit.jiripolasek.com/).

## Package Contents

The `Themes/PillSearchBoxStyle.xaml` dictionary exposes the `PillSearchBoxStyle` resource.

- `JPSoftworks.DesktopToolkit.Styles`
    - `SearchBoxLayout` provides the attached property that places the query icon before the text.

## Documentation

See the full guide for requirements, installation, and usage examples. [WinUI Styles guide](https://desktoptoolkit.jiripolasek.com/components/styles/)

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
