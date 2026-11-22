# JPSoftworks Desktop Toolkit - Toast Windows

Show save confirmations, status updates, and other temporary messages in a WinUI 3 app. Toasts use an acrylic backdrop and open without taking focus.

This package is part of the [JPSoftworks Desktop Toolkit](https://desktoptoolkit.jiripolasek.com/).

## Package Contents

Toasts support placement, a custom display duration, and an optional action button.

- `JPSoftworks.DesktopToolkit.ToastWindows`
    - `ToastWindow` shows notifications.
    - `ToastPosition` sets their placement and `ToastAction` configures a button.
    - `TransparentWindow` and `AlwaysActiveDesktopAcrylicBackdrop` provide window and backdrop helpers.

## Documentation

See the full guide for requirements, installation, and usage examples. [Toast Windows guide](https://desktoptoolkit.jiripolasek.com/components/toasts/)

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
