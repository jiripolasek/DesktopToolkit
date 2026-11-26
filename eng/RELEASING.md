# Releasing the toolkit

One Git tag releases every package in the catalog and the gallery at the same version. Set `Version` in each
`components/*/component.props` and in `gallery/app/JPSoftworks.DesktopToolkit.Gallery.csproj`, then check the package catalog with `./eng/Get-Components.ps1 -Tag v0.1.0-preview.2`.
The check fails if any version differs from the tag.

Pull requests build the Windows solution, run the component tests, and check the package archives.
Full CI also installs packages in the consumer projects that define integration checks, including Native AOT and trimmed builds. It publishes Native AOT and single-file Gallery ZIPs for x64 and ARM64 and checks that the Gallery version matches the catalog.
The [CI workflow](../.github/workflows/ci.yml) lists the configurations it exercises.
Run `./eng/Build.ps1 -TestPackage` on Windows to build, test, and pack the complete toolkit locally.
Run `./eng/Publish-Gallery.ps1` to create both gallery downloads in `artifacts/gallery`, or pass `-Platform x64` or `-Platform ARM64` to build one architecture. Native AOT requires the Visual Studio C++ toolchain for the selected architectures.
Pass `-SingleFile` to create ZIPs containing one self-contained Gallery executable and the license notices. The executable extracts its dependencies on first launch.
Before the first release, set the `NUGET_USER` variable to `jiripolasek` in the `nuget` GitHub environment.
Configure NuGet.org trusted publishing for GitHub repository `jiripolasek/DesktopToolkit`, workflow file `release.yml`, and environment `nuget`.
Allow new packages and versions matching `JPSoftworks.DesktopToolkit.*`.

Wait for full CI on the release commit on `main` to pass and upload its `release-assets` artifact. From that commit, tag the release and push the tag.

```powershell
git tag -a v0.1.0-preview.2 -m "JPSoftworks Desktop Toolkit 0.1.0-preview.2" HEAD
git push origin refs/tags/v0.1.0-preview.2
```

The workflow verifies the CI archive digest, all four versioned Gallery ZIPs, and every package's metadata before NuGet publishing. It uploads Core and waits for NuGet.org to index that version before uploading the standalone controls and their symbols. It publishes the Controls bundle once NuGet.org indexes its five direct dependencies. It attaches the same packages, symbols, and gallery ZIPs to the GitHub release. Prerelease tags create GitHub prereleases.

If publication stops partway through, wait for the accepted packages to appear on NuGet.org and rerun the failed jobs. The workflow compares previously published package contents with the validated files, allows for NuGet.org's repository signature, and resumes missing uploads. A conflicting published version requires a new toolkit version.

## Accessibility and platform verification

Use the gallery or the relevant component's development app for the interactions affected by a release.

- Exercise each changed interaction with the keyboard and Narrator. Check names, state announcements, and where focus goes after dismissal. Examples include choosing a date range, inspecting a chart value, and opening a menu.
- Check text, focus indicators, and layout in High Contrast, at 100% and 200% Windows text scale, with long localized strings and right-to-left layout. For windows and popups, also check movement between monitors with different DPI settings.
- Run the affected packages in a consumer on their minimum supported Windows version and on ARM64. Record the package version, runtime, and architecture exercised.

For `0.1.0-preview.1`, hands-on accessibility and display checks and runtime checks on Windows 10 build 19041 and ARM64 are deferred.

## Documentation site

In the repository's Pages settings, select GitHub Actions as the publishing source. If the `github-pages` environment restricts deployments, allow both `main` and `docs`.

For a local preview, install Node 24 and PowerShell 7. Open the `docs` worktree listed by `git worktree list`, or create one with `git worktree add ../DesktopToolkit-docs docs`. Run `npm ci` in the docs worktree.

To include the API reference, use Windows with the SDK selected by `global.json`, the .NET 10 runtime,
and Windows SDK 10.0.19041.0. Set `COMPONENTS_ROOT` to the library checkout, then run `npm run build:api`.
Run `npm run dev` to start the local preview.
