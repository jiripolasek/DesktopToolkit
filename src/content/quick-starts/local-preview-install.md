### Install the component on its own

If your app already has [Desktop Toolkit Controls](../controls/), continue to the example. The steps below install this component from a local preview feed.

Build the package from the toolkit checkout on Windows with the .NET 11 SDK and the [repository build prerequisites](../../getting-started/#try-the-gallery). From the checkout root, run:

```powershell wrap
dotnet pack "{{project}}" `
    -c Release -p:Platform=x64 -o artifacts/packages
```

Add the checkout's `artifacts/packages` directory to your app's NuGet sources. In Visual Studio, use **Tools > NuGet Package Manager > Package Manager Settings > Package Sources**. Keep your usual sources enabled for dependencies.

From your app's project directory, install the exact preview version:

```powershell wrap
dotnet add package "{{packageId}}" `
    --version "{{version}}"
```
