# Development guide

## Build and run

On Windows, install the .NET 11 SDK selected by [global.json](../global.json), the .NET 10 runtime for control tests, and PowerShell 7. You also need the Windows SDK and MSVC C++ tools for Native AOT package checks.

From the repository root, build the solution, run the control tests, and pack the libraries.

```powershell
./eng/Build.ps1
```

The packages are written to `artifacts/packages`. Add that folder to your NuGet sources to try a local preview. Use `-TestPackage` to run the standalone package consumer checks for components that define them.

Open [the solution](../JPSoftworks.DesktopToolkit.slnx), select x64 or ARM64, and choose the gallery or a component's dev app as the startup project. With WinApp CLI installed, you can also run the gallery from PowerShell.

```powershell
winapp run ./gallery/JPSoftworks.DesktopToolkit.Gallery.csproj --arch x64 --detach
```

## Format code

Run the formatter from the repository root. It restores the repository's pinned ReSharper and XAML Styler tools.

```powershell
./eng/Format.ps1
```

Use `./eng/Format-Modified.ps1` to format new and changed C# and XAML files.

## Repository layout

- `components/<name>/src/` holds the package project and its NuGet README. Component examples, development apps, and tests live in `samples/`, `dev/`, and `tests/` alongside it when present.
- `gallery/` hosts examples from the component sample projects.
- `eng/` contains build scripts and project imports. `NuGetPackage.props` supplies the shared package assets and metadata. `WinUIPackage.props` adds WinUI library settings. `WinUI.props` and `PackagedApp.props` define settings for shared WinUI projects and apps.
- The `docs` branch holds the Astro Starlight site and generates its package pages from the catalog and package READMEs.

## Check a package

```powershell
./eng/Get-Components.ps1
./eng/Test-PackageMetadata.ps1
```

The catalog lists each package and its version. The metadata check opens the packed NuGet archives and verifies their identity, metadata, and embedded assets. Pass a toolkit tag such as `-Tag v0.1.0-preview.1` to check the release set, or a component selector such as `-Tag charts/v0.1.0-preview.1` to check one package locally. Use `-PackageDirectory` to inspect a different output folder.

See the [release guide](RELEASING.md) for tagging and publishing the toolkit.
