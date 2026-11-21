---
title: Localization
description: Translate the Desktop Toolkit's built-in WinUI text with RESW files in your app.
---

Add a `.resw` file to your WinUI app to translate built-in prompts, labels, validation messages, and accessibility text. The controls read the app's resources automatically. Use the same files to customize wording in the app's default language.

## Choose a resource catalog

Put each file under `Strings/<locale>/` in your app project. Use the filenames below as written. Each catalog has its own keys, and its name stays stable across namespace, assembly, and package changes.

| Feature | App resource filename | English template |
| --- | --- | --- |
| [Date and Time Controls](../components/date-time/) | `JPSoftworks.DesktopToolkit.DateTime.resw` | [Date and time control keys](https://github.com/jiripolasek/DesktopToolkit/blob/main/components/DateTime/src/Strings/en-US/JPSoftworks.DesktopToolkit.DateTime.resw) |
| [Searchable menus and dropdowns](../components/searchable-flyouts/) | `JPSoftworks.DesktopToolkit.SearchableFlyouts.resw` | [Searchable flyout keys](https://github.com/jiripolasek/DesktopToolkit/blob/main/components/SearchableFlyouts/src/Strings/en-US/JPSoftworks.DesktopToolkit.SearchableFlyouts.resw) |
| [Key names and shortcut editor](../components/key-visual/) | `JPSoftworks.DesktopToolkit.Keyboard.resw` | [Keyboard keys](https://github.com/jiripolasek/DesktopToolkit/blob/main/components/KeyVisual/src/Strings/en-US/JPSoftworks.DesktopToolkit.Keyboard.resw) |
| [Charts](../components/charts/) | `JPSoftworks.DesktopToolkit.Charts.resw` | [Chart keys](https://github.com/jiripolasek/DesktopToolkit/blob/main/components/Charts/src/Strings/en-US/JPSoftworks.DesktopToolkit.Charts.resw) |
| [Info label](../components/info-label/) | `JPSoftworks.DesktopToolkit.InfoLabel.resw` | [Info label keys](https://github.com/jiripolasek/DesktopToolkit/blob/main/components/InfoLabel/src/Strings/en-US/JPSoftworks.DesktopToolkit.InfoLabel.resw) |

Include only the keys you want to override. Keep the key names as written in the template. Preserve numbered placeholders such as `{0}` in translated format strings.

`DayCountFormat`, `MatchingCommandCountFormat`, and `MatchingChoiceCountFormat` each cover every count. Use wording that works with any number, such as `Days: {0:N0}`.

## Example using CalendarDateRangePicker

Create `Strings/cs-CZ/JPSoftworks.DesktopToolkit.DateTime.resw` in your app project with these entries.

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>
  <resheader name="version"><value>2.0</value></resheader>
  <data name="PlaceholderText" xml:space="preserve">
    <value>Vyberte obdob&#237;</value>
  </data>
  <data name="ApplyButtonText" xml:space="preserve">
    <value>Potvrdit</value>
  </data>
  <data name="DayCountFormat" xml:space="preserve">
    <value>Po&#269;et dn&#237;: {0:N0}</value>
  </data>
</root>
```

In an app that defaults to English, include the same keys in `Strings/en-US/JPSoftworks.DesktopToolkit.DateTime.resw`. You can use the English template values `Select dates`, `Apply`, and `Days: {0:N0}`. A default-language entry for each translated key avoids MakePri warning `PRI263`.

With Czech selected, the picker uses the translated prompt, Apply label, and day count. The preset prompt and other labels use the library defaults. Continue to localize app content such as headers and preset names in your app's own resources.

## Select the language at startup

WinUI uses the app's preferred language. If your app has a saved language setting or you want to test a particular translation, set `PrimaryLanguageOverride` before loading any resources or creating controls. Use a language your app declares support for. [Windows App SDK language override](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.windows.globalization.applicationlanguages.primarylanguageoverride)

```csharp
public App()
{
    Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "cs-CZ";
    this.InitializeComponent();
}
```

After changing the app language at runtime, call `ToolkitLocalization.InvalidateStringResources()`. It clears the string cache and refreshes built-in text on existing controls. Explicit text overrides keep their values.

## Override a single control

Use a property such as `CalendarDateRangePicker.PlaceholderText` or assign the control's `Strings` object when a particular instance needs different wording. Explicit overrides take precedence. Set the picker's `PlaceholderText` or `PresetPlaceholderText` to an empty string to hide that hint. An empty value for either property on its `Strings` object also hides the hint. For each remaining key, the toolkit checks a custom loader if configured, then the app catalog, library resources, and embedded English. Missing and blank catalog entries, lookup errors, and invalid formats fall through to the next source.

## Use a custom string loader

The localized controls read catalogs from the app's PRI and include Core for shared localization configuration. Implement `IStringResourceLoader` to use another source, such as your app's localization service. [Core package](../components/core/)

```csharp
using JPSoftworks.DesktopToolkit.Localization;

public sealed class MyToolkitStrings : IStringResourceLoader
{
    public string? GetString(string catalogName, string key)
    {
        return (catalogName, key) switch
        {
            ("JPSoftworks.DesktopToolkit.DateTime", "PlaceholderText") => "Choose reporting dates",
            _ => null,
        };
    }
}
```

Set `ToolkitLocalization.StringResourceLoader = new MyToolkitStrings();` during startup before loading XAML or reading control defaults. Each assignment invalidates cached lookups. Use the catalog name without its `.resw` extension. Return null for keys to resolve through RESW. Set the loader itself to null to use the built-in sources alone.
