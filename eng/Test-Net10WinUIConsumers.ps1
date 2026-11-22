[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$DotNetSdkVersion,
    [string]$PackageDirectory = "$PSScriptRoot/../artifacts/packages"
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$packages = (Resolve-Path -LiteralPath $PackageDirectory).ProviderPath
$catalog = & "$PSScriptRoot/Get-Components.ps1"
$versions = ([xml](Get-Content -LiteralPath "$PSScriptRoot/../Directory.Packages.props" -Raw)).Project.ItemGroup.PackageVersion
$buildTools = ($versions | Where-Object Include -eq 'Microsoft.Windows.SDK.BuildTools').Version
$validation = [IO.Directory]::CreateTempSubdirectory('dt10-').FullName
Write-Host "Validating .NET 10 WinUI consumers in $validation"

[xml]$config = @'
<configuration>
  <packageSources>
    <clear />
    <add key="local-validation" value="" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="local-validation"><package pattern="JPSoftworks.DesktopToolkit.*" /></packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
  </packageSourceMapping>
  <fallbackPackageFolders><clear /></fallbackPackageFolders>
</configuration>
'@
$config.configuration.packageSources.add[0].SetAttribute('value', $packages)
$configPath = Join-Path $validation 'NuGet.Config'
$config.Save($configPath)
@{ sdk = @{ version = $DotNetSdkVersion; rollForward = 'disable' } } | ConvertTo-Json |
    Set-Content -LiteralPath (Join-Path $validation 'global.json') -Encoding utf8
foreach ($name in 'Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props') {
    Set-Content -LiteralPath (Join-Path $validation $name) -Value '<Project />' -Encoding utf8
}

$cases = [ordered]@{
    Charts = @{ References = 'charts'; WindowsBuild = '26100'; Controls = '<charts:LineChart />' }
    DateTime = @{ References = 'date-time'; WindowsBuild = '19041'; Controls = '<controls:CalendarDateRangePicker />' }
    Combined = @{
        References = 'controls', 'charts'
        WindowsBuild = '26100'
        Controls = '<charts:LineChart /><controls:CalendarDateRangePicker /><controls:SearchableDropDown />'
    }
}

foreach ($case in $cases.GetEnumerator()) {
    foreach ($mode in 'Trimmed', 'Aot') {
        $consumer = [IO.Directory]::CreateDirectory((Join-Path $validation "$($case.Key)-$mode")).FullName
        $publish = Join-Path $consumer 'publish'
        $cache = Join-Path $validation "$($case.Key)-$mode-n"
        $framework = "net10.0-windows10.0.$($case.Value.WindowsBuild).0"
        $aot = $mode -eq 'Aot'
        $minimumRevision = 87
        $projectionSetting = if (!$aot) { "    <WindowsSdkPackageMinimumRevision>$minimumRevision</WindowsSdkPackageMinimumRevision>" }
        $references = @($catalog | Where-Object slug -in $case.Value.References)
        if ($references.Count -ne @($case.Value.References).Count) { throw "Missing package metadata for $($case.Key)." }
        $packageReferences = $references | ForEach-Object {
            '    <PackageReference Include="{0}" Version="{1}" />' -f $_.packageId, $_.version
        }

        @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>$framework</TargetFramework>
    <TargetPlatformMinVersion>10.0.19041.0</TargetPlatformMinVersion>
    <UseWinUI>true</UseWinUI>
    <WinUISDKReferences>false</WinUISDKReferences>
    <WindowsPackageType>None</WindowsPackageType>
    <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
    <SelfContained>true</SelfContained>
    <PublishTrimmed>true</PublishTrimmed>
    <PublishAot>$($aot.ToString().ToLowerInvariant())</PublishAot>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <Nullable>enable</Nullable>
    <CsWinRTAotWarningLevel>3</CsWinRTAotWarningLevel>
    <TrimmerSingleWarn>false</TrimmerSingleWarn>
$projectionSetting
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.260317003" />
    <PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="$buildTools" PrivateAssets="all" />
$($packageReferences -join [Environment]::NewLine)
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath (Join-Path $consumer 'WinUIConsumer.csproj') -Encoding utf8
        @'
<Application x:Class="WinUIConsumer.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Application.Resources><XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" /></Application.Resources>
</Application>
'@ | Set-Content -LiteralPath (Join-Path $consumer 'App.xaml') -Encoding utf8
        @"
<Window x:Class="WinUIConsumer.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:charts="using:JPSoftworks.DesktopToolkit.Charts"
        xmlns:controls="using:JPSoftworks.DesktopToolkit.Controls">
  <StackPanel>$($case.Value.Controls)</StackPanel>
</Window>
"@ | Set-Content -LiteralPath (Join-Path $consumer 'MainWindow.xaml') -Encoding utf8
        @'
using Microsoft.UI.Xaml;

namespace WinUIConsumer;

public partial class App : Application
{
    private Window? _window;

    public App() => this.InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        this._window = new MainWindow();
        this._window.Activate();
    }
}

public sealed partial class MainWindow : Window
{
    public MainWindow() => this.InitializeComponent();
}
'@ | Set-Content -LiteralPath (Join-Path $consumer 'App.xaml.cs') -Encoding utf8

        Push-Location -LiteralPath $consumer
        try {
            $sdk = & dotnet.exe --version
            if ($LASTEXITCODE -ne 0 -or $sdk -ne $DotNetSdkVersion) { throw "Expected SDK $DotNetSdkVersion, found $sdk." }
            & dotnet.exe publish WinUIConsumer.csproj -c Release -r win-x64 -p:Platform=x64 -o $publish -warnaserror -nr:false `
                "-p:RestoreConfigFile=$configPath" "-p:RestorePackagesPath=$cache" "-bl:$consumer/publish.binlog"
            if ($LASTEXITCODE -ne 0) { throw "The $($case.Key) $mode publish failed. See $consumer." }
        } finally { Pop-Location }

        $assets = Get-Content -LiteralPath (Join-Path $consumer 'obj/project.assets.json') -Raw | ConvertFrom-Json -AsHashtable
        $expected = if ($case.Key -eq 'Combined') { $catalog | Where-Object slug -in 'controls', 'charts', 'date-time', 'searchable-flyouts' }
            else { $references }
        foreach ($reference in $expected) {
            $identity = "$($reference.packageId)/$($reference.version)"
            $archive = Join-Path $packages "$($reference.packageId).$($reference.version).nupkg"
            $hash = [Convert]::ToBase64String([Convert]::FromHexString((Get-FileHash -LiteralPath $archive -Algorithm SHA512).Hash))
            if ($assets.libraries[$identity].sha512 -cne $hash) { throw "The consumer did not restore the local $identity archive." }
        }
        if (!(Test-Path -LiteralPath (Join-Path $publish 'WinUIConsumer.exe'))) { throw "Missing $mode app in $publish." }
        $coreClr = Test-Path -LiteralPath (Join-Path $publish 'coreclr.dll')
        $managed = Test-Path -LiteralPath (Join-Path $publish 'WinUIConsumer.dll')
        if ($coreClr -eq $aot -or $managed -eq $aot) { throw "Unexpected $mode runtime layout in $publish." }
        if (!$aot) {
            $deps = Get-Content -LiteralPath (Join-Path $publish 'WinUIConsumer.deps.json') -Raw | ConvertFrom-Json -AsHashtable
            $runtimePack = @($deps.libraries.Keys -like 'runtimepack.Microsoft.Windows.SDK.NET.Ref/*')
            if ($runtimePack.Count -ne 1) { throw "Expected one Windows SDK runtime pack in $publish." }
            $projection = [version]($runtimePack[0].Split('/')[1].Split('-')[0])
            if ($projection.Build -ne [int]$case.Value.WindowsBuild -or $projection.Revision -lt $minimumRevision) {
                throw "Expected Windows SDK build $($case.Value.WindowsBuild) revision $minimumRevision or later, found $projection."
            }
        }
        $cacheDirectory = Get-Item -LiteralPath $cache
        if ($cacheDirectory.Parent.FullName -ne $validation -or $cacheDirectory.Attributes.HasFlag([IO.FileAttributes]::ReparsePoint)) {
            throw "Unexpected NuGet cache path $cache."
        }
        Remove-Item -LiteralPath $cacheDirectory.FullName -Recurse -Force
        Write-Host "$($case.Key) passed the $mode publish on SDK $sdk for Windows $($case.Value.WindowsBuild)."
    }
}
