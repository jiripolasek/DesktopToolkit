# Standard packaging investigation

Preview.1 shipped a Windows SDK projection override to support cold restores on SDK 10.0.404.
Preview.2 retires the manifest transformation and leaves projection selection to the app.
See [Trimming and Native AOT](../src/README.md#trimming-and-native-aot) for the documented app setting.
The experiments below record the earlier packaging decision.
## Repository SDK versus consumer SDK

The repository uses SDK 11 to build its multi-targeted library and development solution. That source-build requirement
is separate from the SDK used by a developer consuming the published NuGet package. A .NET 10 consuming app can use
SDK 10 (verified here with 10.0.404); it is not required to upgrade to SDK 11. A .NET 11 app requires SDK 11.
The preview.1 transformation provided automatic projection handling for that SDK. Neither choice
requires installed-app users to install an SDK; the host application's deployment determines runtime prerequisites.

## Scope

All consumers used Windows App SDK 1.8.260317003, x64, Release, and warnings as errors. The library retained
Microsoft.WindowsAppSDK.WinUI 1.8.260224000 and Microsoft.Windows.CsWinRT 2.3.1 throughout. Tests used actual
NuGet package bytes and generated standalone consumers. No package was published to a feed.

At the time of the experiments, the library targeted net10.0-windows10.0.19041 and net11.0-windows10.0.19041. The experiments did not
change target frameworks, control code, resource paths, or the minimum supported Windows version.

## Experiments

| Package | Change | Consumer | Result |
| --- | --- | --- | --- |
| 0.1.0-standard.1 | Bypass manifest transformation; keep the explicit 19041.87 dependency and existing consumer target | SDK 11 / .NET 10 / Windows 19041, trimmed | Passed; projection 19041.87 |
| 0.1.0-standard.1 | Same | SDK 11 / .NET 10 / Windows 26100, Native AOT, cold restore | Failed with duplicate generated WinRT definitions (CS0101, CS0111, CS0579) |
| 0.1.0-standard.2 | Remove explicit projection dependency and transformation; use WindowsSdkPackageMinimumRevision=87 in the library and a small consumer target | SDK 11 / .NET 10 / Windows 26100, Native AOT, cold restore | Passed; projection 26100.87 |
| 0.1.0-standard.2 | Same | SDK 11 / .NET 11 / Windows 26100, trimmed | Passed; projection 26100.87 |
| 0.1.0-standard.2 | Same | SDK 10.0.404 / .NET 10 / Windows 26100, trimmed, cold restore | Failed with NETSDK1112: Windows SDK runtime pack was not downloaded |
| 0.1.0-standard.2 | App explicitly sets WindowsSdkPackageVersion=10.0.26100.87 before restore | SDK 10.0.404 / .NET 10 / Windows 26100, trimmed, cold restore | Passed; requires app-side configuration |
| 0.1.0-transform.1 | Restored implementation | SDK 10.0.404 / .NET 10 / Windows 26100, Native AOT and trimmed, each with cold restore | Both passed; projection 26100.87 |
| 0.1.0-transform.1 | Restored implementation | SDK 11.0.100 / .NET 11 / Windows 26100, Native AOT and trimmed | Both passed; projection 26100.87 |

## Why the SDK-native minimum is insufficient on a cold restore

The .NET SDK supports WindowsSdkPackageMinimumRevision. Its task raises a projection's revision while retaining
the Windows build number. It also rejects setting that property together with an explicit WindowsSdkPackageVersion.
See the [SDK implementation](https://source.dot.net/Microsoft.NET.Build.Tasks/CreateWindowsSdkKnownFrameworkReferences.cs.html).

The standard.2 consumer target executes before AddWindowsSdkKnownFrameworkReferences, but a newly restored package's
build target was not available to the original restore graph. SDK 10.0.404 requested a download of 10.0.26100.57.
The later build required revision 87 and failed because its runtime pack was absent. SDK 11's default projection
already satisfied this floor in the successful experiments, so testing only SDK 11 hid the regression.

The transformed package's restore graph also requested the SDK's default download, but its matching NuGet dependency
group additionally restored Microsoft.Windows.SDK.NET.Ref/10.0.26100.87. That made the required revision available
on the first publish. The two project.assets.json comparisons are recorded in restore-evidence.json.

Simply bypassing the transformation while keeping the explicit 19041 projection dependency leaves a 19041-specific
dependency group selected for newer Windows targets. In the measured 26100 consumer this produced duplicate WinRT
generator definitions. The SDK-native experiment removed that dependency as well, which resolved that failure but
exposed the older SDK's first-restore problem.

## Preview.1 decision

The release retained the transformation and shipped projection-selection target for automatic setup on SDK 10.
An explicit app-side WindowsSdkPackageVersion=10.0.26100.87 made standard.2 pass a cold trimmed publish on SDK
10.0.404. This confirms that standard packaging can retain WinUI 1.8 if consumers accept an explicit SDK projection
setting. It does not preserve the current automatic setup. The initial optional pin trial exhausted temporary-disk
space; the completed retry used an isolated workspace-drive cache. Only caches created by this investigation were
removed to recover temporary disk space.

Preview.2 removes the producer-side targets, XSLT, and consumer projection hook. The consumer target continues to copy compiled XAML.

## Verification and reproduction

The restored package passed four standalone publishes: Native AOT and trimmed on SDK 10.0.404/.NET 10, and Native AOT
and trimmed on SDK 11.0.100/.NET 11. All four used WinUI 1.8.260317003, selected the matching library framework,
retained five compiled XAML resources, verified the package-byte hash, and produced AMD64 executables. AOT outputs
contained neither the managed app DLL nor CoreCLR; trimmed outputs contained both.

Test-Package.ps1 now accepts -DotNetSdkVersion. It creates an exact SDK selector with roll-forward disabled in the
generated consumer and runs publishing from that directory, avoiding the repository's SDK 11 selector. It validates
and records the actual SDK version. It never installs an SDK or changes the repository/host SDK configuration.

The commands below reproduce the historical publishes with a preview.1 package at an absolute path.

~~~powershell
foreach ($flavor in 'Aot', 'Trimmed') {
    ./components/SearchableFlyouts/scripts/Test-Package.ps1 -PackagePath $candidate `
        -TargetFramework net10.0-windows10.0.26100.0 -WindowsAppSdkVersion 1.8.260317003 `
        -DotNetSdkVersion 10.0.404 -Flavor $flavor -ColdRestore
    ./components/SearchableFlyouts/scripts/Test-Package.ps1 -PackagePath $candidate `
        -TargetFramework net11.0-windows10.0.26100.0 -WindowsAppSdkVersion 1.8.260317003 `
        -DotNetSdkVersion 11.0.100 -Flavor $flavor
}
~~~

The standard.1 package was built with -p:_FlyoutsBaseManifest=true. The standard.2 variant removed the projection
PackageReference and WindowsSdkDependencies.targets import, set the library's WindowsSdkPackageMinimumRevision
to 87 on .NET 10, and replaced the shipped nuspec lookup with a conditional target setting that same floor.
Both experimental packages, the restored control package, pack binlogs, publish logs, and copied result JSON are in
artifacts/standard-packaging. The experimental standard.2 sources are also embedded in standard2-pack.binlog.

This verification covers restore, compilation, linking, and published artifacts on x64. The published apps were
not launched for UI interaction, and ARM64/runtime coverage on Windows build 19041 was not repeated here.
