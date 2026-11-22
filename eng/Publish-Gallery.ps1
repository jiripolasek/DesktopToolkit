#requires -Version 7.4

[CmdletBinding()]
param(
    [ValidateSet('x64', 'ARM64')][string[]]$Platform = @('x64', 'ARM64'),
    [string]$OutputDirectory = "$PSScriptRoot/../artifacts/gallery"
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$name = 'JPSoftworks.DesktopToolkit.Gallery'
$project = Join-Path $root "gallery/app/$name.csproj"
$output = [IO.Directory]::CreateDirectory($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory))
$version = ([xml](Get-Content -Raw -LiteralPath $project)).SelectSingleNode('/Project/PropertyGroup/Version').InnerText
& "$PSScriptRoot/Get-Components.ps1" -Tag "v$version" | Out-Null

function Copy-RuntimeNotices($Assets, [string]$Rid, [string]$Layout) {
    $target = $Assets.targets.GetEnumerator() | Where-Object { $_.Key.EndsWith("/$Rid") } | Select-Object -First 1
    foreach ($entry in $target.Value.GetEnumerator()) {
        if ($entry.Value.type -ne 'package' -or !($entry.Value.runtime -or $entry.Value.native)) { continue }
        $library = $Assets.libraries[$entry.Key]
        $notices = @($library.files -match '^(license|notice|third[-_ ]?party[-_ ]?notices?)(\.(txt|md))?$')
        if (!$notices.Count) { continue }
        $package = $Assets.packageFolders.Keys | ForEach-Object { Join-Path $_ $library.path } |
            Where-Object { Test-Path -LiteralPath $_ -PathType Container } | Select-Object -First 1
        $destination = [IO.Directory]::CreateDirectory((Join-Path $Layout "ThirdParty/$($entry.Key.Split('/')[0])"))
        foreach ($file in $notices) { Copy-Item -LiteralPath (Join-Path $package $file) -Destination $destination.FullName }
    }
}

# MakePRI needs short intermediate paths when embedding compiled XAML.
$work = [IO.Directory]::CreateTempSubdirectory('dtg-')
try {
    foreach ($arch in $Platform) {
        $rid = "win-$($arch.ToLowerInvariant())"
        $layout = Join-Path $work.FullName $rid
        & dotnet publish $project -c Release -r $rid "-p:Platform=$arch" -p:PublishPortableGallery=true --artifacts-path $work.FullName -o $layout
        if ($LASTEXITCODE -ne 0) { throw "Gallery publish failed for $rid." }

        foreach ($file in @("$name.exe", 'resources.pri', 'Microsoft.UI.Xaml.dll', 'Microsoft.Graphics.Canvas.dll', 'WinUIEditor.dll', 'Assets/HomeHero.jpg')) {
            if (!(Test-Path -LiteralPath (Join-Path $layout $file) -PathType Leaf)) { throw "Missing $rid gallery file: $file" }
        }
        $stream = [IO.File]::OpenRead((Join-Path $layout "$name.exe"))
        try {
            $reader = [System.Reflection.PortableExecutable.PEReader]::new($stream)
            try { $machine = $reader.PEHeaders.CoffHeader.Machine } finally { $reader.Dispose() }
        } finally { $stream.Dispose() }
        $expectedMachine = if ($arch -eq 'x64') { 'Amd64' } else { 'Arm64' }
        if ($machine.ToString() -ne $expectedMachine) { throw "Expected a $rid gallery executable, found $machine." }

        foreach ($file in 'LICENSE', 'NOTICE.md') { Copy-Item -LiteralPath (Join-Path $root $file) -Destination $layout }
        $assets = Get-Content -Raw -LiteralPath (Join-Path $work.FullName "obj/$name/project.assets.json") | ConvertFrom-Json -AsHashtable
        Copy-RuntimeNotices $assets $rid $layout
        Get-ChildItem -LiteralPath $layout -File -Recurse -Filter '*.pdb' | Remove-Item
        $asset = Join-Path $output.FullName "$name-$version-$rid.zip"
        $partial = "$asset.$([guid]::NewGuid().ToString('N')).tmp"
        try {
            [IO.Compression.ZipFile]::CreateFromDirectory($layout, $partial)
            [IO.File]::Move($partial, $asset, $true)
        } finally {
            [IO.File]::Delete($partial)
        }
        Write-Host "Created $asset"
    }
} finally {
    if ($work.Parent.FullName -eq [IO.Path]::TrimEndingDirectorySeparator([IO.Path]::GetTempPath())) { $work.Delete($true) }
}
