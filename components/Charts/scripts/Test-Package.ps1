[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PackagePath,
    [ValidateSet('x64', 'ARM64', 'x86')][string]$Architecture = 'x64',
    [ValidateSet('Aot', 'Jit')][string]$Flavor = 'Aot',
    [switch]$ColdRestore
)

$ErrorActionPreference = 'Stop'
$componentRoot = Split-Path -Parent $PSScriptRoot
$root = Split-Path -Parent (Split-Path -Parent $componentRoot)
$id = 'JPSoftworks.DesktopToolkit.Controls.Charts'
$package = (Get-Item -LiteralPath $PackagePath).FullName
$validation = Join-Path ([IO.Path]::GetTempPath()) ('charts-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
$feed = Join-Path $validation 'feed'
New-Item -ItemType Directory -Path $feed -Force | Out-Null
Copy-Item -LiteralPath $package -Destination $feed
$archive = [IO.Compression.ZipFile]::OpenRead($package)
try {
    $library = "lib/net8.0-windows10.0.19041/$id"
    foreach ($entry in @("$library.dll", "$library.xml", "$library.pri", "$library/Themes/Generic.xaml",
        'LICENSE', 'licenses/DevDrive.LICENSE.txt', 'README.md')) {
        if (!$archive.GetEntry($entry)) { throw "Missing $entry in the Charts package." }
    }
    $reader = [IO.StreamReader]::new($archive.GetEntry("$id.nuspec").Open())
    try { [xml]$spec = $reader.ReadToEnd() } finally { $reader.Dispose() }
} finally {
    $archive.Dispose()
}
$version = $spec.package.metadata.version
$dependencies = @($spec.package.metadata.dependencies.group.dependency)
$legacySdk = ($dependencies | Where-Object id -eq 'Microsoft.WindowsAppSDK').version
if (!$legacySdk -or $dependencies.id -notcontains 'Microsoft.Graphics.Win2D' -or
    $dependencies.id -contains 'Microsoft.Windows.SDK.BuildTools' -or
    $spec.package.metadata.license.InnerText -ne 'Apache-2.0') {
    throw 'Unexpected Charts package dependencies or license.'
}
[xml]$central = Get-Content -LiteralPath (Join-Path $root 'Directory.Packages.props') -Raw
$gallerySdk = ($central.Project.ItemGroup.PackageVersion | Where-Object Include -eq 'Microsoft.WindowsAppSDK').Version
$buildTools = ($central.Project.ItemGroup.PackageVersion | Where-Object Include -eq 'Microsoft.Windows.SDK.BuildTools').Version

$config = Join-Path $validation 'NuGet.Config'
$escaped = [Security.SecurityElement]::Escape($feed)
@"
<configuration>
  <packageSources>
    <clear />
    <add key="charts" value="$escaped" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="charts"><package pattern="$id" /></packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
  </packageSourceMapping>
</configuration>
"@ | Set-Content -LiteralPath $config

$restoreArgs = @("-p:RestoreConfigFile=$config")
if ($ColdRestore) {
    $restoreArgs += "-p:RestorePackagesPath=$validation/packages"
}

function Invoke-Dotnet([string[]]$Arguments) {
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($Arguments[0]) failed. See $validation." }
}

function Invoke-Check([string]$Exe, [string[]]$Arguments) {
    $process = Start-Process -FilePath $Exe -WorkingDirectory (Split-Path -Parent $Exe) `
        -ArgumentList $Arguments -WindowStyle Hidden -PassThru
    if (!$process.WaitForExit(60000)) {
        Stop-Process -Id $process.Id
        throw "Charts check timed out. See $validation."
    }
    if ($process.ExitCode -ne 0) { throw "Charts check failed with exit $($process.ExitCode). See $validation." }
}

function Assert-RestoredPackage([string]$AssetsPath) {
    $assets = Get-Content -LiteralPath $AssetsPath -Raw | ConvertFrom-Json
    $library = $assets.libraries."$id/$version"
    if ($library.type -ne 'package') { throw "Missing Charts package reference in $AssetsPath." }
    $name = [IO.Path]::GetFileName($package)
    $restored = $assets.packageFolders.PSObject.Properties.Name | ForEach-Object {
        Join-Path $_ "$($library.path)/$($name.ToLowerInvariant())"
    } | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
    if (!$restored -or (Get-FileHash -LiteralPath $restored).Hash -ne (Get-FileHash -LiteralPath $package).Hash) {
        throw "The restored Charts artifact does not match $package."
    }
}

function Test-Standalone([string]$Framework, [string]$Sdk, [bool]$Aot) {
    $mode = if ($Aot) { 'aot' } else { 'net8' }
    $consumer = Join-Path $validation $mode
    New-Item -ItemType Directory -Path $consumer | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $componentRoot 'tests/PackageConsumer') -File |
        Copy-Item -Destination $consumer
    $project = Join-Path $consumer 'ChartsPackageConsumer.csproj'
    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <TargetFramework>$Framework</TargetFramework>
    <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
    <UseWinUI>true</UseWinUI>
    <WinUISDKReferences>false</WinUISDKReferences>
    <WindowsPackageType>None</WindowsPackageType>
    <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
    <SelfContained>true</SelfContained>
    <PublishAot>$($Aot.ToString().ToLowerInvariant())</PublishAot>
    <RuntimeIdentifier>win-$($Architecture.ToLowerInvariant())</RuntimeIdentifier>
    <Platform>$Architecture</Platform>
    <ImplicitUsings>enable</ImplicitUsings>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <Manifest Include="app.manifest" />
    <PackageReference Include="$id" Version="$version" />
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="$Sdk" />
    <PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="$buildTools" PrivateAssets="all" />
  </ItemGroup>
  <Target Name="CopyProbeXamlToPublish" AfterTargets="Publish" Condition="'`$(PublishAot)' == 'true'">
    <ItemGroup>
      <ProbeXaml Include="`$(TargetDir)App.xbf;`$(TargetDir)ChartPage.xbf;`$(TargetDir)`$(AssemblyName).pri" />
    </ItemGroup>
    <Copy SourceFiles="@(ProbeXaml)" DestinationFolder="`$(PublishDir)" SkipUnchangedFiles="true" />
  </Target>
</Project>
"@ | Set-Content -LiteralPath $project
    if ($Aot) {
        $out = Join-Path $consumer 'publish'
        Invoke-Dotnet (@('publish', $project, '-c', 'Release', '-o', $out, '-warnaserror',
            "-bl:$consumer/publish.binlog") + $restoreArgs)
        $exe = Join-Path $out 'ChartsPackageConsumer.exe'
    } else {
        Invoke-Dotnet (@('build', $project, '-c', 'Release', '-warnaserror',
            "-bl:$consumer/build.binlog") + $restoreArgs)
        $exe = (Get-ChildItem -LiteralPath (Join-Path $consumer 'bin') -Recurse -File -Filter 'ChartsPackageConsumer.exe' |
            Select-Object -First 1).FullName
    }

    Assert-RestoredPackage (Join-Path $consumer 'obj/project.assets.json')
    $report = Join-Path $consumer 'result.txt'
    Invoke-Check $exe @('"' + $report + '"')
    if ((Get-Content -LiteralPath $report -Raw) -ne 'Passed') {
        throw "Standalone $mode rendering did not pass. See $report."
    }
    Write-Host "The $mode package consumer loaded chart templates and drew Win2D surfaces twice."
}

Write-Host "Validating $id $version at $validation"
$dev = Join-Path $componentRoot 'dev/JPSoftworks.DesktopToolkit.Controls.Charts.DevApp.csproj'
$devOutput = Join-Path $validation 'dev'
Invoke-Dotnet (@('build', $dev, '-c', 'Release', "-p:Platform=$Architecture", '-p:EnableSmokeTests=true',
    '-p:UseChartsPackage=true', "-p:ChartsPackageVersion=$version", '-p:UseArtifactsOutput=true',
    "-p:ArtifactsPath=$devOutput", "-bl:$validation/dev.binlog") + $restoreArgs)
Assert-RestoredPackage (Join-Path $devOutput "obj/JPSoftworks.DesktopToolkit.Controls.Charts.DevApp/project.assets.json")
$devExe = Get-ChildItem -LiteralPath (Join-Path $devOutput 'bin') -Filter 'JPSoftworks.DesktopToolkit.Controls.Charts.DevApp.exe' -Recurse -File |
    Select-Object -First 1
if (!$devExe) { throw "The package smoke host was not built. See $validation." }
$report = Join-Path $validation 'smoke-results.json'
Invoke-Check $devExe.FullName @('--smoke-test', ('"' + $report + '"'))
$results = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
if (!$results.Passed -or !$results.Checks.Count) { throw "The package smoke checks failed. See $report." }
Write-Host "Passed $($results.Checks.Count) chart smoke checks."
Test-Standalone 'net8.0-windows10.0.26100.0' $legacySdk $false
if ($Flavor -eq 'Aot') {
    Test-Standalone 'net11.0-windows10.0.26100.0' $gallerySdk $true
}
