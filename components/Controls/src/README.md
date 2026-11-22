# Controls

Add the toolkit's general-purpose WinUI controls and styles with one package reference.

## Requirements

- Windows 10 version 2004 (build 19041) or later
- A WinUI 3 app targeting .NET 8 or later
- Windows App SDK 1.8.260317003 or later

### .NET 10 trimmed publishing

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

## Installation

For a local preview, run `./eng/Build.ps1` from the toolkit checkout and add `artifacts/packages` to your NuGet sources.
Keep your regular sources enabled for dependencies.

Run this command from your WinUI app project directory.

```powershell
dotnet add package JPSoftworks.DesktopToolkit.Controls --version 0.1.0-preview.2
```

## Usage

CalendarDateRangePicker, InfoLabel, KeyVisual, and Searchable Flyouts share one XAML namespace whether you install the bundle or reference the individual packages. Declare `xmlns:controls="using:JPSoftworks.DesktopToolkit.Controls"` once on the page and use the same prefix for all four.

```xml
<StackPanel xmlns:controls="using:JPSoftworks.DesktopToolkit.Controls" Orientation="Horizontal">
    <controls:CalendarDateRangePicker Header="Report period" />
    <controls:InfoLabel AutomationProperties.Name="About the report period"
                        Description="The report includes both the start and end dates." />
</StackPanel>
```

## Included packages

The metapackage includes five packages.

| Package | Features |
| --- | --- |
| `JPSoftworks.DesktopToolkit.Controls.DateTime` | Calendar date range picker with presets and period comparison |
| `JPSoftworks.DesktopToolkit.Controls.InfoLabel` | Contextual help beside fields and labels |
| `JPSoftworks.DesktopToolkit.Controls.KeyVisual` | Keycaps, shortcut presentation, and shortcut editing |
| `JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts` | Searchable menus, item flyouts, and dropdowns |
| `JPSoftworks.DesktopToolkit.Styles` | Opt-in templates for built-in WinUI controls |

Reference an individual package if your app only needs one feature. Add `JPSoftworks.DesktopToolkit.Controls.Charts` for charts and gauges or `JPSoftworks.DesktopToolkit.ToastWindows` for toast windows.
