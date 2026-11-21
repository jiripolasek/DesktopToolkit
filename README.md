<div align="center">

<img src="art/toolkit-icon.png" alt="" width="128" height="128">

<h1>JPSoftworks Desktop Toolkit<br/><small>for WinUI</small></h1>

</div>



Build WinUI 3 desktop apps with [interactive charts](#charts), [in-app toast windows](#toast-windows), and [reusable controls and styles](#controls-and-styles). The gallery includes live examples and their source code. The [documentation site](https://desktoptoolkit.jiripolasek.com/) includes the package guides and API reference.

## Component highlights

<div align="center">
<img src="art/gallery.png" alt="Overlapping Gallery screenshots showing the Desktop Toolkit banner, a Sankey chart, a searchable menu with its filter at the top, and an expanded date range picker">
</div>

## NuGet packages

| Component guide | NuGet package ID | Version |
| --- | --- | --- |
| [Controls bundle](components/Controls/src/README.md) | `JPSoftworks.DesktopToolkit.Controls` | `0.1.0-preview.1` |
| [Charts](components/Charts/src/README.md) | `JPSoftworks.DesktopToolkit.Controls.Charts` | `0.1.0-preview.1` |
| [Toast Windows](components/Toasts/src/README.md) | `JPSoftworks.DesktopToolkit.ToastWindows` | `0.1.0-preview.1` |
| [DateRangePicker](components/DateRangePicker/src/README.md) | `JPSoftworks.DesktopToolkit.Controls.DateRangePicker` | `0.1.0-preview.1` |
| [InfoLabel](components/InfoLabel/src/README.md) | `JPSoftworks.DesktopToolkit.Controls.InfoLabel` | `0.1.0-preview.1` |
| [KeyVisual](components/KeyVisual/src/README.md) | `JPSoftworks.DesktopToolkit.Controls.KeyVisual` | `0.1.0-preview.1` |
| [Searchable Flyouts](components/SearchableFlyouts/src/README.md) | `JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts` | `0.1.0-preview.1` |
| [WinUI Styles](components/Styles/src/README.md) | `JPSoftworks.DesktopToolkit.Styles` | `0.1.0-preview.1` |

### Controls and styles

The [Controls bundle](components/Controls/src/README.md) installs these five packages together. Charts and Toast Windows install separately.

- **Searchable Flyouts** adds filtering to command menus and dropdowns, including virtualized lists for large item sets.
- **KeyVisual** renders keycaps, presents shortcuts inline, and lets users capture and edit keyboard shortcuts.
- **DateRangePicker** selects date ranges with presets and previous-period comparison.
- **InfoLabel** adds a native help tooltip beside a field or label.
- **WinUI Styles** provides opt-in templates such as a pill-shaped settings search box.

### Charts

Build dashboards and data views with [Charts](components/Charts/src/README.md). Compare series with bar, line, and scatter charts. Show flows and hierarchies with Sankey charts, treemaps, and sunbursts. Monitor changing values with live area graphs, usage meters, and radial gauges.

### Toast Windows

Show save confirmations, status messages, or a report link with [Toast Windows](components/Toasts/src/README.md). These acrylic windows open without taking keyboard focus, dismiss automatically, and can include an action button. They follow the Windows on-screen indicator position by default.

## Installation

For a local preview, [build the packages locally](eng/README.md#build-and-run) and add `artifacts/packages` to your NuGet sources. Keep your regular sources enabled for dependencies.

Run the commands for the packages you want to use from your WinUI app project. The Controls bundle supports .NET 10 and .NET 11 on Windows 10 version 2004 or later. See the package guides above for Charts and Toast Windows requirements.

```powershell
dotnet add package JPSoftworks.DesktopToolkit.Controls --version 0.1.0-preview.1
dotnet add package JPSoftworks.DesktopToolkit.Controls.Charts --version 0.1.0-preview.1
dotnet add package JPSoftworks.DesktopToolkit.ToastWindows --version 0.1.0-preview.1
```

## Development

See the [development guide](eng/README.md) to run the gallery, build the packages, and work on a component. See the [release guide](eng/RELEASING.md) for CI, NuGet publishing, and the documentation site.

For bug reports and pull requests, see [Contributing](CONTRIBUTING.md). Package changes are listed in the [changelog](CHANGELOG.md).

## License

The Desktop Toolkit is licensed under the [Apache License 2.0](LICENSE). [Third-party notices](NOTICE.md) accompany the relevant source and packages.

## Author

[Jiří Polášek](https://jiripolasek.com)
