# Changelog

## 0.1.0-preview.2

Initial preview of JPSoftworks Desktop Toolkit for WinUI 3.

- **Controls** bundles Date and Time Controls, InfoLabel, KeyVisual, Searchable Flyouts, and WinUI Styles for .NET 8 and later.
- **Date and Time Controls** supports presets, blackout policies, custom flyout content, and calendar styling.
- **InfoLabel** shows plain-text and formatted help beside a field or label.
- **KeyVisual** displays keycaps and lets users edit keyboard shortcuts.
- **Searchable Flyouts** filters commands and choices in menus, flyouts, and dropdowns.
- **Trimmed .NET 10 builds**. Apps using any Desktop Toolkit WinUI package can set `WindowsSdkPackageMinimumRevision` to `87` if a trimmed publish reports `IL2081` in Windows SDK projection code. Searchable Flyouts and Controls now rely on the app's projection setting, so apps that relied on the automatic override may need this property.
- **WinUI Styles** includes `PillSearchBoxStyle` in `Themes/PillSearchBoxStyle.xaml`.
- **Charts** provides interactive charts, usage meters, gauges, and detached legends.
- **Toast Windows** shows acrylic in-app notifications.
- **Gallery** includes live examples with source code and self-contained x64 and ARM64 ZIPs.
- The documentation site provides package guides and an API reference. [Documentation](https://desktoptoolkit.jiripolasek.com/)
