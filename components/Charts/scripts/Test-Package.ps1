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
$coreId = 'JPSoftworks.DesktopToolkit.Core'
$package = (Get-Item -LiteralPath $PackagePath).FullName
$validation = Join-Path ([IO.Path]::GetTempPath()) ('charts-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
$feed = Join-Path $validation 'feed'
New-Item -ItemType Directory -Path $feed -Force | Out-Null
Copy-Item -LiteralPath $package -Destination $feed
$archive = [IO.Compression.ZipFile]::OpenRead($package)
try {
    foreach ($framework in 'net8.0', 'net10.0', 'net11.0') {
        $library = "lib/$framework-windows10.0.19041/$id"
        foreach ($entry in @("$library.dll", "$library.xml", "$library.pri", "$library/Themes/Generic.xaml")) {
            if (!$archive.GetEntry($entry)) { throw "Missing $entry in the Charts package." }
        }
    }
    foreach ($entry in 'LICENSE', 'licenses/DevDrive.LICENSE.txt') {
        if (!$archive.GetEntry($entry)) { throw "Missing $entry in the Charts package." }
    }
    $reader = [IO.StreamReader]::new($archive.GetEntry("$id.nuspec").Open())
    try { [xml]$spec = $reader.ReadToEnd() } finally { $reader.Dispose() }
} finally {
    $archive.Dispose()
}
$version = $spec.package.metadata.version
$dependencies = @($spec.package.metadata.dependencies.group.dependency)
$coreVersion = @($dependencies | Where-Object id -eq $coreId | ForEach-Object version | Sort-Object -Unique)
if ($coreVersion.Count -ne 1 -or
    $dependencies.id -notcontains 'Microsoft.Graphics.Win2D' -or
    $dependencies.id -notcontains 'Microsoft.WindowsAppSDK.WinUI' -or
    $dependencies.id -notcontains 'Microsoft.Windows.CsWinRT' -or
    $dependencies.id -contains 'Microsoft.WindowsAppSDK' -or
    $dependencies.id -contains 'Microsoft.Windows.SDK.BuildTools' -or
    $spec.package.metadata.license.InnerText -ne 'Apache-2.0') {
    throw 'Unexpected Charts package dependencies or license.'
}
$dependencyArchive = Join-Path (Split-Path -Parent $package) "$coreId.$($coreVersion[0]).nupkg"
Copy-Item -LiteralPath $dependencyArchive -Destination $feed
$galleryProject = Join-Path $root 'gallery/app/JPSoftworks.DesktopToolkit.Gallery.csproj'
$evaluation = & dotnet msbuild $galleryProject '-getItem:PackageVersion' '-p:Configuration=Release' "-p:Platform=$Architecture"
if ($LASTEXITCODE -ne 0) { throw 'Cannot evaluate the Gallery package versions.' }
$centralVersions = ($evaluation | Out-String | ConvertFrom-Json).Items.PackageVersion
$gallerySdk = ($centralVersions | Where-Object Identity -eq 'Microsoft.WindowsAppSDK').Version
$buildTools = ($centralVersions | Where-Object Identity -eq 'Microsoft.Windows.SDK.BuildTools').Version
if (!$gallerySdk -or !$buildTools) { throw 'Missing Windows App SDK or Build Tools version.' }

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
    <packageSource key="charts"><package pattern="$id" /><package pattern="$coreId" /></packageSource>
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

function Assert-RestoredPackage([string]$AssetsPath, [string]$ExpectedFramework) {
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
    if ($ExpectedFramework) {
        $runtime = "win-$($Architecture.ToLowerInvariant())"
        $target = $assets.targets.PSObject.Properties | Where-Object Name -like "$ExpectedFramework-windows*/$runtime"
        $selected = $target.Value."$id/$version"
        $expectedAsset = "lib/$ExpectedFramework-windows10.0.19041/$id.dll"
        if ($expectedAsset -notin $selected.compile.PSObject.Properties.Name -or
            $expectedAsset -notin $selected.runtime.PSObject.Properties.Name) {
            throw "NuGet did not select $expectedAsset in $AssetsPath."
        }
    }
}

function Test-Standalone([string]$Framework, [bool]$Aot, [string]$WindowsAppSdkVersion) {
    $frameworkVersion = $Framework.Split('-')[0]
    $mode = if ($Aot) { "$frameworkVersion-aot" } else { "$frameworkVersion-jit" }
    $consumer = Join-Path $validation $mode
    New-Item -ItemType Directory -Path $consumer | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $componentRoot 'tests/PackageConsumer') -File |
        Copy-Item -Destination $consumer
    $project = Join-Path $consumer 'ChartsPackageConsumer.csproj'
    $sdkReference = if ($WindowsAppSdkVersion) {
        '    <PackageReference Include="Microsoft.WindowsAppSDK" Version="{0}" />' -f $WindowsAppSdkVersion
    } else { '' }
    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <TargetFramework>$Framework</TargetFramework>
    <TargetPlatformMinVersion>10.0.19041.0</TargetPlatformMinVersion>
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
$sdkReference
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

    $assetsPath = Join-Path $consumer 'obj/project.assets.json'
    Assert-RestoredPackage $assetsPath $frameworkVersion
    if (!$WindowsAppSdkVersion) {
        $assets = Get-Content -LiteralPath $assetsPath -Raw | ConvertFrom-Json -AsHashtable
        if ($assets.libraries.Keys -like 'Microsoft.WindowsAppSDK/*') {
            throw "The $mode consumer restored the full Windows App SDK. See $assetsPath."
        }
    }
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
Test-Standalone 'net8.0-windows10.0.26100.0' $false
if ($Flavor -eq 'Aot') {
    Test-Standalone 'net10.0-windows10.0.26100.0' $true
    Test-Standalone 'net11.0-windows10.0.26100.0' $true $gallerySdk
}
