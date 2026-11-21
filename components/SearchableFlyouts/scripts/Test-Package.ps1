param(
    [ValidateSet('Aot', 'Trimmed', 'Archive')][string]$Flavor = 'Aot',
    [ValidateSet('x64', 'ARM64')][string]$Architecture = 'x64',
    [string]$TargetFramework = 'net10.0-windows10.0.26100.0',
    [string]$OutputDirectory = (Join-Path ([IO.Path]::GetTempPath()) 'wuipc'),
    [string]$WindowsAppSdkVersion,
    [string]$PackagePath,
    [string]$WindowsSdkPackageVersion,
    [string]$DotNetSdkVersion,
    [switch]$ColdRestore
)
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$packageId = 'JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts'
$projectionId = 'Microsoft.Windows.SDK.NET.Ref'
$runId = [guid]::NewGuid().ToString('N')
$validationRoot = Join-Path ([IO.Path]::GetFullPath($OutputDirectory, $PWD.ProviderPath)) ('pkg-' + $runId.Substring(0, 12))
$consumer = Join-Path $validationRoot 'consumer'
$packages = Join-Path $validationRoot 'packages'
$sample = Join-Path $workspace 'dev'
$xamlNames = @('ItemStyles', 'MenuFilterBox', 'MenuStyles', 'SearchableDropDown', 'SearchableItemsFlyout')
$version = '0.1.0-validation.' + (Get-Date -Format 'yyyyMMddHHmmss') + '.' + $runId.Substring(0, 8)

function Invoke-Dotnet([string[]]$Arguments) {
    & dotnet.exe @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed with exit code $LASTEXITCODE." }
}

function Read-PackageXml([IO.Compression.ZipArchive]$Archive, [string]$Path) {
    $entry = $Archive.GetEntry($Path)
    if (!$entry) { throw "Missing $Path in the package." }
    $stream = $entry.Open()
    try {
        $document = [xml]::new()
        $document.Load($stream)
        return ,$document
    } finally { $stream.Dispose() }
}

if ($PackagePath) {
    $archivePath = (Get-Item -LiteralPath $PackagePath).FullName
} else {
    New-Item -ItemType Directory -Path $packages -Force | Out-Null
    Invoke-Dotnet @('pack', (Join-Path $workspace "src/$packageId.csproj"),
        '-c', 'Release', '-p:Platform=x64', "-bl:$validationRoot/package.binlog", "-p:PackageVersion=$version", '-o', $packages, '-warnaserror')
    $archivePath = Join-Path $packages "$packageId.$version.nupkg"
}

$archive = [IO.Compression.ZipFile]::OpenRead($archivePath)
try {
    $nuspec = Read-PackageXml $archive "$packageId.nuspec"
    $version = $nuspec.package.metadata.version
    $dependencyFrameworks = @($nuspec.package.metadata.dependencies.group | ForEach-Object targetFramework)
    $assemblies = @($archive.Entries | Where-Object FullName -like "lib/*/$packageId.dll")
    $frameworks = @($assemblies | ForEach-Object { $_.FullName.Split('/')[1] })
    $targets = @($archive.Entries | Where-Object FullName -like "buildTransitive/*/$packageId.targets")
    if (!$frameworks.Count -or $targets.Count -ne $frameworks.Count -or
        $archive.GetEntry("buildTransitive/$packageId.targets")) {
        throw 'Each library assembly must have matching compiled XAML build targets.'
    }
    foreach ($framework in $frameworks) {
        if ($framework -notin $dependencyFrameworks) { throw "Missing dependency group for $framework." }
        $library = "lib/$framework/$packageId"
        $build = "buildTransitive/$framework"
        foreach ($path in @("$library.dll", "$library.pri", "$build/$packageId.targets")) {
            if (!$archive.GetEntry($path)) { throw "Missing $path in the package." }
        }
        foreach ($name in $xamlNames) {
            foreach ($path in @("$library/Controls/$name.xaml", "$build/Controls/$name.xbf")) {
                if (!$archive.GetEntry($path)) { throw "Missing $path in the package." }
            }
        }
        $documentation = Read-PackageXml $archive "$library.xml"
        foreach ($control in @('SearchableMenuFlyout', 'SearchableItemsFlyout', 'SearchableDropDown')) {
            if (!$documentation.SelectSingleNode("/doc/members/member[@name='T:JPSoftworks.DesktopToolkit.Controls.$control']/summary")) {
                throw "Missing XML documentation for $control in $framework."
            }
        }
    }
} finally { $archive.Dispose() }
if ($Flavor -eq 'Archive') { return }
New-Item -ItemType Directory -Path $consumer, $packages -Force | Out-Null
if ($PackagePath) {
    Copy-Item -LiteralPath $archivePath -Destination (Join-Path $packages "$packageId.$version.nupkg")
}

foreach ($file in @('App.xaml', 'App.xaml.cs', 'MainWindow.xaml', 'MainWindow.xaml.cs',
    'MainPage.xaml', 'MainPage.xaml.cs', 'app.manifest', 'Package.appxmanifest')) {
    Copy-Item -LiteralPath (Join-Path $sample $file) -Destination $consumer
}
Copy-Item -LiteralPath (Join-Path $sample 'Assets') -Destination $consumer -Recurse
# Inline repository-owned defaults and role imports into the standalone project.
function Add-ConsumerProjectContent([string]$Path, [xml]$Destination) {
    [xml]$source = Get-Content -LiteralPath $Path -Raw
    foreach ($node in $source.Project.ChildNodes) {
        if ($node.Name -eq 'Import') {
            if ($node.HasAttribute('Condition')) { throw "Cannot export a conditional import in $Path." }
            $directory = [IO.Path]::GetDirectoryName($Path) + [IO.Path]::DirectorySeparatorChar
            $import = $node.Project.Replace('$(MSBuildThisFileDirectory)', $directory)
            if ($import.Contains('$(')) { throw "Cannot export unresolved import $import." }
            Add-ConsumerProjectContent ([IO.Path]::GetFullPath($import, $directory)) $Destination
        } else {
            $Destination.Project.AppendChild($Destination.ImportNode($node, $true)) | Out-Null
        }
    }
}

$sampleProject = Join-Path $sample 'JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts.DevApp.csproj'
$evaluation = & dotnet.exe msbuild $sampleProject '-getItem:PackageReference,PackageVersion' `
    "-p:Platform=$Architecture" '-p:Configuration=Release' "-bl:$validationRoot/consumer-evaluation.binlog"
if ($LASTEXITCODE -ne 0) { throw 'Cannot evaluate the development app dependencies.' }
$evaluated = ($evaluation | Out-String | ConvertFrom-Json -AsHashtable).Items
$centralVersions = @{}
foreach ($item in $evaluated.PackageVersion) { $centralVersions[$item.Identity] = $item.Version }
$referenceVersions = @{}
foreach ($item in $evaluated.PackageReference) {
    $referenceVersions[$item.Identity] = if ($item.VersionOverride) { $item.VersionOverride }
        elseif ($item.Version) { $item.Version } else { $centralVersions[$item.Identity] }
}
[xml]$project = '<Project Sdk="Microsoft.NET.Sdk" />'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $workspace '../..'))
Add-ConsumerProjectContent (Join-Path $repositoryRoot 'Directory.Build.props') $project
Add-ConsumerProjectContent $sampleProject $project
foreach ($reference in $project.SelectNodes('//PackageReference')) {
    $dependencyVersion = $referenceVersions[$reference.Include]
    if (!$dependencyVersion) { throw "Missing explicit version for $($reference.Include)." }
    $reference.RemoveAttribute('VersionOverride')
    $reference.SetAttribute('Version', $dependencyVersion)
}
if (!$WindowsAppSdkVersion) { $WindowsAppSdkVersion = $referenceVersions['Microsoft.WindowsAppSDK'] }
$project.SelectSingleNode('//PackageReference[@Include="Microsoft.WindowsAppSDK"]').SetAttribute('Version', $WindowsAppSdkVersion)
# Stop automatic imports when the output directory is inside another repository.
foreach ($name in 'Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props') {
    [IO.File]::WriteAllText((Join-Path $consumer $name), "<Project />" + [Environment]::NewLine)
}
foreach ($reference in @($project.SelectNodes('//ProjectReference'))) {
    $reference.ParentNode.RemoveChild($reference) | Out-Null
}
$properties = $project.CreateElement('PropertyGroup')
$consumerProperties = @{
    TargetFramework = $TargetFramework
    SelfContained = 'true'
    ManagePackageVersionsCentrally = 'false'
    PublishAot = (($Flavor -eq 'Aot').ToString().ToLowerInvariant())
    PublishTrimmed = 'true'
    PublishReadyToRun = 'false'
}
if ($WindowsSdkPackageVersion) { $consumerProperties.WindowsSdkPackageVersion = $WindowsSdkPackageVersion }
foreach ($entry in $consumerProperties.GetEnumerator()) {
    $property = $project.CreateElement($entry.Key)
    $property.InnerText = $entry.Value
    $properties.AppendChild($property) | Out-Null
}
$project.Project.AppendChild($properties) | Out-Null
$references = $project.CreateElement('ItemGroup')
$reference = $project.CreateElement('PackageReference')
$reference.SetAttribute('Include', $packageId)
$reference.SetAttribute('Version', $version)
$references.AppendChild($reference) | Out-Null
$project.Project.AppendChild($references) | Out-Null
$projectPath = Join-Path $consumer 'JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts.DevApp.csproj'
$project.Save($projectPath)
$config = [xml]'<configuration><packageSources><add key="local-validation" value="../packages" /></packageSources></configuration>'
if ($ColdRestore) {
    $fallbacks = $config.CreateElement('fallbackPackageFolders')
    $fallbacks.AppendChild($config.CreateElement('clear')) | Out-Null
    $config.DocumentElement.AppendChild($fallbacks) | Out-Null
}
$config.Save((Join-Path $consumer 'NuGet.Config'))

$runtime = 'win-' + $Architecture.ToLowerInvariant()
$publishArgs = @('publish', $projectPath, '-c', 'Release', '-r', $runtime,
    "-p:Platform=$Architecture", "-bl:$validationRoot/consumer-publish.binlog", '-warnaserror', '-v:minimal', '-nr:false')
if ($ColdRestore) { $publishArgs += "-p:RestorePackagesPath=$validationRoot/n" }
if ($DotNetSdkVersion) {
    @{ sdk = @{ version = $DotNetSdkVersion; rollForward = 'disable' } } | ConvertTo-Json |
        Set-Content -LiteralPath (Join-Path $consumer 'global.json') -Encoding utf8
    Push-Location $consumer
}
try {
    $actualDotNetSdkVersion = (& dotnet.exe --version | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve the consumer .NET SDK.' }
    if ($DotNetSdkVersion -and $actualDotNetSdkVersion -ne $DotNetSdkVersion) {
        throw "Expected SDK $DotNetSdkVersion, found $actualDotNetSdkVersion."
    }
    Invoke-Dotnet $publishArgs
} finally {
    if ($DotNetSdkVersion) { Pop-Location }
}
$assets = Get-Content -LiteralPath (Join-Path $consumer 'obj/project.assets.json') -Raw | ConvertFrom-Json -AsHashtable
$sha512 = [Convert]::ToBase64String([Convert]::FromHexString((Get-FileHash -LiteralPath $archivePath -Algorithm SHA512).Hash))
if ($assets.libraries["$packageId/$version"].sha512 -cne $sha512) { throw 'NuGet restored different package bytes.' }
$restoredFramework = @($assets.project.frameworks.Keys)[0]
$selected = $assets.targets["$restoredFramework/$runtime"]["$packageId/$version"]
$compile = @($selected.compile.Keys | Where-Object { $_ -like "lib/*/$packageId.dll" })
if ($compile.Count -ne 1) { throw "Expected one compile assembly for $restoredFramework." }
$libraryFramework = $compile[0].Split('/')[1]
$exactFramework = $frameworks | Where-Object { $_ -eq $restoredFramework -or "$_.0" -eq $restoredFramework }
if ($exactFramework -and $libraryFramework -ne $exactFramework) {
    throw "NuGet selected $libraryFramework for $restoredFramework instead of $exactFramework."
}
$matchingFrameworks = @($frameworks -like "$($restoredFramework.Split('-')[0])-*")
if ($matchingFrameworks.Count -and $libraryFramework -notin $matchingFrameworks) {
    throw "NuGet selected $libraryFramework for $restoredFramework instead of the matching framework."
}
if ($compile[0] -notin $selected.runtime.Keys -or "buildTransitive/$libraryFramework/$packageId.targets" -notin $selected.build.Keys) {
    throw "NuGet selected inconsistent compile, runtime, or XAML assets for $restoredFramework."
}
$output = Join-Path $consumer "bin/$Architecture/Release/$TargetFramework/$runtime"
$dependencies = Get-Content -LiteralPath (Join-Path $output 'JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts.DevApp.deps.json') -Raw | ConvertFrom-Json -AsHashtable
$runtimePack = @($dependencies.libraries.Keys -like "runtimepack.$projectionId/*")
if ($runtimePack.Count -ne 1) { throw "Expected one Windows SDK runtime pack for $restoredFramework." }
$projectionVersion = $runtimePack[0].Split('/')[1]
if ($WindowsSdkPackageVersion -and $projectionVersion -ne $WindowsSdkPackageVersion) {
    throw "The app selected Windows SDK $projectionVersion instead of the requested $WindowsSdkPackageVersion."
}
$requiredProjection = $selected.dependencies[$projectionId]
$exactDependencyGroup = $dependencyFrameworks | Where-Object { $_ -eq $restoredFramework -or "$_.0" -eq $restoredFramework }
if ($requiredProjection -and $exactDependencyGroup -and !$WindowsSdkPackageVersion) {
    $minimum = [version]$requiredProjection.Trim('[', ']')
    $actual = [version]$projectionVersion.Split('-')[0]
    if ($actual.Build -ne $minimum.Build -or $actual -lt $minimum) {
        throw "Expected Windows SDK build $($minimum.Build) revision $($minimum.Revision) or later, found $projectionVersion."
    }
}
$publish = Join-Path $output 'publish'
$executable = Join-Path $publish 'JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts.DevApp.exe'
$stream = [IO.File]::OpenRead($executable)
try {
    $pe = [System.Reflection.PortableExecutable.PEReader]::new($stream)
    try {
        $machine = $pe.PEHeaders.CoffHeader.Machine.ToString()
        $expectedMachine = @{ x64 = 'Amd64'; ARM64 = 'Arm64' }[$Architecture]
        if ($machine -ne $expectedMachine) { throw "The $Architecture publish produced a $machine executable." }
        $isNative = $null -eq $pe.PEHeaders.CorHeader
        if ($Flavor -eq 'Aot' -and -not $isNative) { throw 'The published executable is managed.' }
    } finally { $pe.Dispose() }
} finally { $stream.Dispose() }
$managedApp = Test-Path -LiteralPath (Join-Path $publish 'JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts.DevApp.dll')
$coreClr = Test-Path -LiteralPath (Join-Path $publish 'coreclr.dll')
if ($Flavor -eq 'Aot' -and ($managedApp -or $coreClr)) { throw 'The AOT output contains a managed application or CoreCLR.' }
if ($Flavor -eq 'Trimmed' -and (-not $managedApp -or -not $coreClr)) { throw 'The trimmed JIT application is incomplete.' }
$xbf = @(Get-ChildItem -LiteralPath (Join-Path $publish $packageId) -Filter '*.xbf' -Recurse)
if ($xbf.Count -ne $xamlNames.Count) { throw "Expected $($xamlNames.Count) compiled XAML resources, found $($xbf.Count)." }
foreach ($name in $xamlNames) {
    if (!(Test-Path -LiteralPath (Join-Path $publish "$packageId/Controls/$name.xbf"))) {
        throw "The published app cannot resolve $packageId/Controls/$name.xaml."
    }
}
$report = [ordered]@{
    timestampUtc = [DateTime]::UtcNow.ToString("O")
    flavor = $Flavor
    targetFramework = $TargetFramework
    packageFrameworks = $frameworks
    libraryFramework = $libraryFramework
    requestedDotNetSdkVersion = $DotNetSdkVersion
    dotNetSdkVersion = $actualDotNetSdkVersion
    windowsAppSdkVersion = $WindowsAppSdkVersion
    windowsSdkReference = $projectionVersion
    architecture = $Architecture
    executableMachine = $machine
    packageVersion = $version
    packageSha256 = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash
    executableSha256 = (Get-FileHash -LiteralPath $executable -Algorithm SHA256).Hash
    nativeExecutable = $isNative
    managedAppAssemblyPresent = $managedApp
    coreClrPresent = $coreClr
    compiledXamlResources = $xbf.Count
    executable = $executable
    publishDirectory = $publish
    manifest = (Join-Path $output 'AppxManifest.xml')
    stagingDirectory = (Join-Path $validationRoot 'staging')
}
$reportJson = $report | ConvertTo-Json
[IO.File]::WriteAllText((Join-Path $validationRoot 'publish-results.json'), $reportJson + [Environment]::NewLine)
$reportJson
Write-Output 'Launch the published directory with winapp run using the manifest and staging paths above, then run the three UI scripts against its PID.'
