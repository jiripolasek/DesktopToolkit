# JPSoftworks Desktop Toolkit - Controls

Add date range picking, contextual help, keyboard shortcuts, searchable menus, and WinUI styles to your app with one package reference.

This package is part of the [JPSoftworks Desktop Toolkit](https://desktoptoolkit.jiripolasek.com/).

## Package Contents

The bundle brings in Date and Time Controls, InfoLabel, KeyVisual, Searchable Flyouts, and WinUI Styles.

- `JPSoftworks.DesktopToolkit.Controls`
    - `CalendarDateRangePicker` for choosing dates and `InfoLabel` for field-level help.
    - `KeyVisual`, `ShortcutPresenter`, and `ShortcutEditor` for keyboard shortcuts.
    - `SearchableMenuFlyout`, `SearchableItemsFlyout`, and `SearchableDropDown` for filtering commands and choices.
- `JPSoftworks.DesktopToolkit.Styles`
    - `SearchBoxLayout` supports the `PillSearchBoxStyle` resource.

## Documentation

See the full guide for requirements, installation, and usage examples. [Controls guide](https://desktoptoolkit.jiripolasek.com/components/controls/)

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
