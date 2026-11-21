# WinUI Styles

`JPSoftworks.DesktopToolkit.Styles` provides opt-in styles for built-in WinUI 3 controls.

## Settings search box

`SettingsSearchBoxStyle` gives an `AutoSuggestBox` a pill shape and places its magnifier before the text. It uses the control's built-in clear button and query and suggestion events. The design is inspired by [PowerToys Command Palette Settings](https://github.com/microsoft/PowerToys/blob/main/src/modules/cmdpal/Microsoft.CmdPal.UI/Settings/SettingsWindow.xaml#L49-L74).

Reference the package and add the dictionary to your app's merged dictionaries after `XamlControlsResources`.

```xml
<ResourceDictionary Source="ms-appx:///JPSoftworks.DesktopToolkit.Styles/Themes/SettingsSearchBoxStyle.xaml" />
```

Apply the style to an `AutoSuggestBox` and set its query icon.

```xml
<AutoSuggestBox Style="{StaticResource SettingsSearchBoxStyle}"
                Header="Search settings"
                PlaceholderText="Find a setting"
                QueryIcon="Find" />
```

Open **Styles > Search box** in the Desktop Toolkit Gallery to try the style and view its XAML and C# source.

## Requirements

The package targets .NET 10 and .NET 11 on Windows 10, version 2004 or later, with WinUI 3.
