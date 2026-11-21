# Charts

The `JPSoftworks.DesktopToolkit.Controls.Charts` package targets .NET 8 and uses Windows App SDK 1.8
and Win2D. The shared gallery lists each chart and scenario in the sidebar.
The standalone Charts dev app uses the same pages on Windows App SDK 1.8.

| Path | Contents |
| --- | --- |
| `src/` | Public controls, templates, XML API docs, and the [consumer guide](src/README.md) |
| `samples/` | The chart examples, dashboards, and sample data shared by both apps |
| `dev/` | The unpackaged chart development app |
| `tests/` | Rendering, interaction, and package-only consumer checks |
| `scripts/Test-Package.ps1` | Validation of the packed artifact in separate consumers |

Build the solution and run the Charts package checks from the repository root.

```powershell
./eng/Build.ps1 -ReleaseTag charts/v0.1.0-preview.1 -TestPackage
```

The checks run the full development app suite using the packed Charts library. They also build and
run a standalone .NET 8 consumer. The default `Aot` flavor then publishes and runs a .NET 11 Native
AOT consumer on the Windows App SDK version used by the shared gallery.
Charts ships with the other packages in each [toolkit release](../../eng/RELEASING.md).
