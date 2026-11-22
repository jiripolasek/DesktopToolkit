# WinUI Styles

Apply opt-in styles to built-in WinUI 3 controls.

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
dotnet add package JPSoftworks.DesktopToolkit.Styles --version 0.1.0-preview.2
```

## Usage

### Pill search box

`PillSearchBoxStyle` gives an `AutoSuggestBox` a pill shape and places its magnifier before the text. It uses the control's built-in clear button and query and suggestion events.

Add the dictionary to your app's merged dictionaries after `XamlControlsResources`.

```xml
<ResourceDictionary Source="ms-appx:///JPSoftworks.DesktopToolkit.Styles/Themes/PillSearchBoxStyle.xaml" />
```

Apply the style to an `AutoSuggestBox` and set its query icon.

```xml
<AutoSuggestBox Style="{StaticResource PillSearchBoxStyle}"
                Header="Search settings"
                PlaceholderText="Find a setting"
                QueryIcon="Find" />
```

Open **Styles > Pill search box** in the Desktop Toolkit Gallery to try the style and view the XAML that adds it to an app.
