# JPSoftworks Desktop Toolkit - Searchable Flyouts

Help users find commands and choices in WinUI 3 by typing to filter menus, item flyouts, and dropdowns.

This package is part of the [JPSoftworks Desktop Toolkit](https://desktoptoolkit.jiripolasek.com/).

## Package Contents

Choose a native menu for existing commands or an item-based control for data-driven lists.

- `JPSoftworks.DesktopToolkit.Controls`
    - `SearchableMenuFlyout` filters native menu items.
    - `SearchableItemsFlyout` filters data-driven commands and recycles rows.
    - `SearchableDropDown` filters choices and commits a single selection.
    - `SearchableFlyoutItem` represents a command or choice.

## Documentation

See the full guide for requirements, installation, and usage examples. [Searchable Flyouts guide](https://desktoptoolkit.jiripolasek.com/components/searchable-flyouts/)

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
