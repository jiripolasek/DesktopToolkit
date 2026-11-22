# InfoLabel

Place a small information icon beside a label, field, or heading to offer optional context. The native WinUI tooltip
opens on hover, keyboard focus, or press and hold.

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
dotnet add package JPSoftworks.DesktopToolkit.Controls.InfoLabel --version 0.1.0-preview.2
```

## Usage

Place an `InfoLabel` beside the content it describes and set its `Description`.

```xml
<StackPanel xmlns:info="using:JPSoftworks.DesktopToolkit.Controls"
            Orientation="Horizontal">
    <TextBlock Text="Refresh interval (seconds)" VerticalAlignment="Center" />
    <info:InfoLabel Description="Shorter intervals use more network data."
                    AutomationProperties.Name="About the refresh interval" />
</StackPanel>
```

Set `AutomationProperties.Name` to identify the adjacent content for screen reader users. The default name is
"More information."

### Formatted tooltip

Set `DescriptionContent` to a XAML element to show formatted help. `Description` supplies the default automation help
text and the tooltip text when `DescriptionContent` is `null`. Both properties support binding.

```xml
<info:InfoLabel xmlns:info="using:JPSoftworks.DesktopToolkit.Controls"
                AutomationProperties.Name="About the request timeout"
                Description="Slow requests are canceled after the timeout.">
    <info:InfoLabel.DescriptionContent>
        <StackPanel MaxWidth="240" Spacing="4">
            <TextBlock FontWeight="Bold" Text="Request timeout" />
            <TextBlock Text="Slow requests are canceled after the timeout." TextWrapping="Wrap" />
        </StackPanel>
    </info:InfoLabel.DescriptionContent>
</info:InfoLabel>
```
