# Desktop Toolkit Controls

Install `JPSoftworks.DesktopToolkit.Controls` to manage the toolkit's general-purpose WinUI controls and styles with one package reference.

```powershell
dotnet add package JPSoftworks.DesktopToolkit.Controls --prerelease
```

For a local preview, build the packages with `./eng/Build.ps1` from the toolkit checkout and add `artifacts/packages` to your NuGet sources before installing. Keep your usual NuGet sources enabled for dependencies.

The metapackage includes five packages.

| Package | Features |
| --- | --- |
| `JPSoftworks.DesktopToolkit.Controls.DateRangePicker` | Date ranges, presets, and period comparison |
| `JPSoftworks.DesktopToolkit.Controls.InfoLabel` | Contextual help beside fields and labels |
| `JPSoftworks.DesktopToolkit.Controls.KeyVisual` | Keycaps, shortcut presentation, and shortcut editing |
| `JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts` | Searchable menus, item flyouts, and dropdowns |
| `JPSoftworks.DesktopToolkit.Styles` | Opt-in templates for built-in WinUI controls |

Reference an individual package if your app only needs one feature. Add `JPSoftworks.DesktopToolkit.Controls.Charts` for charts and gauges or `JPSoftworks.DesktopToolkit.ToastWindows` for toast windows.

## Requirements

Use a WinUI 3 app targeting .NET 10 or .NET 11 and Windows 10 version 2004 or later. The package supports Windows App SDK 1.8.260317003 or later. Searchable Flyouts handles the required Windows SDK projection through its package dependencies.
