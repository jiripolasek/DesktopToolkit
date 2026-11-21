---
title: Developing a component
description: Work on a control, run its tests, and prepare its package for a toolkit release.
---

A component directory contains one packable project under `src/`. Keep its public XAML examples in
`samples/`, its manual reproduction window in `dev/`, and its automated checks in `tests/`.
Open the component's dev app directly when debugging keyboard focus, flyout positioning, or a specific interaction.
The gallery references the sample library. A sample library references the component through its public API.

`component.props` owns the package ID, version, display name, release stage, documentation slug, and verification entry points.
Run `./eng/Get-Components.ps1 -Json` from the library checkout to inspect the catalog used by release automation and this site.
All packages ship together under a single `v<version>` tag. The Controls bundle publishes after its dependencies are available on NuGet.org.

Run `./eng/Build.ps1 -TestPackage` on Windows to build the toolkit, run the component tests, pack the libraries, and exercise the available package consumers.
The [toolkit release guide](https://github.com/jiripolasek/DesktopToolkit/blob/main/eng/RELEASING.md#accessibility-and-platform-verification) describes the accessibility and platform checks for changed components.
