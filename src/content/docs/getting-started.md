---
title: Getting started
description: Install the Desktop Toolkit Controls package and try it in a WinUI app.
---

## Install Desktop Toolkit Controls

The Controls bundle supports .NET 8, .NET 10, and .NET 11. Run this command from your WinUI 3 app's project directory.

```powershell
dotnet add package JPSoftworks.DesktopToolkit.Controls --version 0.1.0-preview.2
```

The [Controls package](../components/controls/) bundles [Date and Time Controls](../components/date-time/), InfoLabel, KeyVisual, Searchable Flyouts, and Styles. Use CalendarDateRangePicker from Date and Time Controls to choose a date range. Add [Charts](../components/charts/) or [Toast Windows](../components/toasts/) when your app needs those features. You can install any component on its own using its ID and version in the [package index](../packages/).

NuGet resolves the shared WinUI dependencies when you install Charts and Toast Windows together. Controls and Charts support Windows App SDK `1.8.260317003`. If you add Toast Windows, check its [Windows App SDK requirements](../components/toasts/#requirements).

## Troubleshoot .NET 10 trimmed publishing

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

## Try a control in your app

Use a shared XAML namespace for CalendarDateRangePicker, InfoLabel, KeyVisual, and Searchable Flyouts.

```xml
xmlns:controls="using:JPSoftworks.DesktopToolkit.Controls"
```

- [Show keyboard shortcuts with KeyVisual](../components/key-visual/#quick-start)
- [Filter a native menu with Searchable Flyouts](../components/searchable-flyouts/#quick-start)

Both examples work with the Controls package. Browse the [control catalog](../catalog/) for more examples.

To translate built-in prompts, labels, and accessibility text in your app, follow the [localization guide](../localization/).

## Try the gallery

Open `JPSoftworks.DesktopToolkit.slnx` on Windows with the .NET 11 SDK, the .NET 8 and .NET 10 runtimes for control tests, and the Windows SDK and C++ development tools installed. Choose the platform that matches your machine, set **JPSoftworks.DesktopToolkit.Gallery** as the startup project, and press F5.

The gallery uses project references to run the code in your checkout. Browse its controls, charts, and scenarios to see examples before adding a package to your app.

## Build packages locally

To try changes from a checkout, run this command in PowerShell using the [gallery build prerequisites](#try-the-gallery).

```powershell
./eng/Build.ps1
```

Add the checkout's `artifacts/packages` directory to your NuGet sources. In Visual Studio, go to **Tools > NuGet Package Manager > Package Manager Settings > Package Sources**. Keep your regular NuGet sources enabled for dependencies.
