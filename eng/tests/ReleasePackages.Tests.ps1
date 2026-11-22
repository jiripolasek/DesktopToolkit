#requires -Version 7.4

$ErrorActionPreference = 'Stop'
$publish = Join-Path $PSScriptRoot '../Publish-ReleasePackages.ps1'
$catalog = @(& "$PSScriptRoot/../Get-Components.ps1")
$tag = "v$($catalog[0].version)"
$bundle = $catalog | Where-Object slug -eq 'controls'
$bundleName = "$($bundle.packageId).$($bundle.version).nupkg"
$core = $catalog | Where-Object slug -eq 'core'
$coreName = "$($core.packageId).$($core.version).nupkg"
$folder = [IO.Directory]::CreateTempSubdirectory('DesktopToolkit-release-test-')
$accepted = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::OrdinalIgnoreCase)
$uploads = [Collections.Generic.List[string]]::new()
$waits = [Collections.Generic.List[int]]::new()
$firstIndexChecks = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$indexed = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$failedIndex = $null
$oldKey = $env:NUGET_API_KEY

function Invoke-WebRequest([uri]$Uri, [string]$Method = 'Get', [string]$OutFile, [switch]$PassThru, [switch]$SkipHttpErrorCheck) {
    $name = [IO.Path]::GetFileName($Uri.AbsolutePath)
    if ($Method -eq 'Head') {
        if (!$accepted.ContainsKey($name)) { throw "Checked dependency before upload: $name" }
        if ($name -eq $failedIndex) { return @{ StatusCode = 503 } }
        if ($firstIndexChecks.Add($name)) { return @{ StatusCode = 404 } }
        [void]$indexed.Add($name)
        return @{ StatusCode = 200 }
    }
    if (!$accepted.ContainsKey($name)) { return @{ StatusCode = 404 } }
    [IO.File]::Copy($accepted[$name], $OutFile, $true)
    return @{ StatusCode = 200 }
}

function Start-Sleep([int]$Seconds) { $waits.Add($Seconds) }

function dotnet {
    $path = $args[2]
    $name = [IO.Path]::GetFileName($path)
    if ($args[0] -ne 'nuget' -or $args[1] -ne 'push') { throw 'Unexpected dotnet command.' }
    if ($name.EndsWith('.snupkg')) {
        if ($args -notcontains '--skip-duplicate') { throw 'Symbol uploads must tolerate retries.' }
    } else {
        if ($args -notcontains '--no-symbols') { throw 'Package and symbol uploads must be separate.' }
        if ($name -eq $leafName -and !$indexed.Contains($coreName)) { throw 'Uploaded a dependent control before Core was indexed.' }
        if ($name -eq $bundleName -and !$indexed.Contains($leafName)) { throw 'Uploaded the bundle before its control was indexed.' }
        $accepted.Add($name, $path)
    }
    $uploads.Add($name)
    $global:LASTEXITCODE = 0
}

try {
    $env:NUGET_API_KEY = 'local-test'
    $catalogWithTag = @(& "$PSScriptRoot/../Get-Components.ps1" -Tag $tag)
    if ($catalogWithTag.Count -ne $catalog.Count) { throw 'A release tag must select the full catalog.' }
    try {
        & "$PSScriptRoot/../Get-Components.ps1" -Tag 'v0.0.0-mismatch' | Out-Null
        throw 'Accepted a tag that does not match the catalog.'
    } catch {
        if (!$_.Exception.Message.Contains('must match every package version')) { throw }
    }

    $leaf = $catalog | Where-Object slug -eq 'charts'
    $leafName = "$($leaf.packageId).$($leaf.version).nupkg"
    $lastSymbol = "$($leaf.packageId).$($leaf.version).snupkg"
    foreach ($component in $catalog) {
        $baseName = "$($component.packageId).$($component.version)"
        $dependency = if ($component.slug -eq 'controls') {
            "<dependencies><group><dependency id='$($leaf.packageId)' version='$($leaf.version)' /></group></dependencies>"
        } elseif ($component.slug -eq $leaf.slug) {
            "<dependencies><group><dependency id='$($core.packageId)' version='$($core.version)' /></group></dependencies>"
        }
        $manifest = "<package><metadata><id>$($component.packageId)</id><version>$($component.version)</version>$dependency</metadata></package>"
        $archive = [IO.Compression.ZipFile]::Open((Join-Path $folder.FullName "$baseName.nupkg"), 'Create')
        try {
            $writer = [IO.StreamWriter]::new($archive.CreateEntry("$($component.packageId).nuspec").Open())
            try { $writer.Write($manifest) } finally { $writer.Dispose() }
        } finally { $archive.Dispose() }
        if ($component.hasSymbols -and "$baseName.snupkg" -ne $lastSymbol) {
            [IO.File]::WriteAllText((Join-Path $folder.FullName "$baseName.snupkg"), 'symbols')
        }
    }

    try {
        & $publish -Tag $tag -PackageDirectory $folder.FullName
        throw 'Accepted an incomplete release.'
    } catch {
        if (!$_.Exception.Message.Contains('Missing release files')) { throw }
    }
    if ($uploads.Count) { throw 'Uploaded a package before validating the complete set.' }
    [IO.File]::WriteAllText((Join-Path $folder.FullName $lastSymbol), 'symbols')

    & $publish -Tag $tag -PackageDirectory $folder.FullName
    $symbolCount = @($catalog | Where-Object hasSymbols).Count
    if ($accepted.Count -ne $catalog.Count -or $uploads.Count -ne $catalog.Count + $symbolCount -or
        $uploads[0] -cne $coreName -or $uploads[-1] -cne $bundleName -or $waits.Count -ne 2) {
        throw 'Expected all packages and symbols, an indexing retry for each dependency, Core first, and the Controls bundle last.'
    }

    $uploads.Clear()
    & $publish -Tag $tag -PackageDirectory $folder.FullName
    if ($uploads.Count -ne $symbolCount -or @($uploads | Where-Object { !$_.EndsWith('.snupkg') }).Count) {
        throw 'A rerun must reuse matching packages and retry the symbol uploads.'
    }
    $uploads.Clear()
    $accepted.Clear()
    $indexed.Clear()
    $failedIndex = $coreName
    try {
        & $publish -Tag $tag -PackageDirectory $folder.FullName
        throw 'Continued publishing after Core indexing failed.'
    } catch {
        if (!$_.Exception.Message.Contains('HTTP 503')) { throw }
    }
    if ($uploads.Count -ne 1 -or $uploads[0] -cne $coreName) { throw 'A Core indexing failure must stop dependent uploads.' }
    Write-Host 'Passed complete toolkit release, ordering, and retry checks.'
} finally {
    $env:NUGET_API_KEY = $oldKey
    foreach ($file in $folder.GetFiles()) { $file.Delete() }
    $folder.Delete()
}
