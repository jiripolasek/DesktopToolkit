#requires -Version 7.4

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PackagePath,
    [switch]$CheckDependencies,
    [ValidateRange(0, 3600)][int]$DependencyWaitSeconds = 0
)

$ErrorActionPreference = 'Stop'

function Get-PackageUri([string]$Id, [string]$Version) {
    $idPath = $Id.ToLowerInvariant()
    $versionPath = $Version.Split('+')[0].ToLowerInvariant()
    [uri]"https://api.nuget.org/v3-flatcontainer/$idPath/$versionPath/$idPath.$versionPath.nupkg"
}

function Get-PackageContents([IO.Compression.ZipArchive]$Archive) {
    $hashes = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
    foreach ($entry in $Archive.Entries) {
        # NuGet.org adds its repository signature after accepting the package.
        if ($entry.FullName -ceq '.signature.p7s') { continue }
        $stream = $entry.Open()
        try {
            $hashes.Add($entry.FullName, [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)))
        } finally { $stream.Dispose() }
    }
    return ,$hashes
}

$local = [IO.Compression.ZipFile]::OpenRead((Get-Item -LiteralPath $PackagePath).FullName)
try {
    $manifest = @($local.Entries | Where-Object { $_.FullName -notlike '*/*' -and $_.Name -like '*.nuspec' })
    if ($manifest.Count -ne 1) { throw "Expected one package manifest in $PackagePath." }
    $stream = $manifest[0].Open()
    try {
        $document = [xml]::new()
        $document.Load($stream)
    } finally { $stream.Dispose() }
    $metadata = $document.package.metadata

    if ($CheckDependencies) {
        $deadline = [DateTime]::UtcNow.AddSeconds($DependencyWaitSeconds)
        $dependencies = @($metadata.dependencies.group.dependency | Sort-Object id, version -Unique)
        if (!$dependencies.Count) { throw 'The Controls bundle contains no dependencies.' }
        foreach ($dependency in $dependencies) {
            $version = [string]$dependency.version
            if ($version -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$') {
                throw "Expected a component version for $($dependency.id), found '$version'."
            }
            $uri = Get-PackageUri $dependency.id $version
            $response = Invoke-WebRequest -Uri $uri -Method Head -SkipHttpErrorCheck
            while ($response.StatusCode -eq 404 -and [DateTime]::UtcNow -lt $deadline) {
                Write-Host "Waiting for NuGet.org to index $($dependency.id) $version."
                Start-Sleep -Seconds 15
                $response = Invoke-WebRequest -Uri $uri -Method Head -SkipHttpErrorCheck
            }
            if ($response.StatusCode -ne 200) {
                throw "Controls requires $($dependency.id) $version to be available before release (HTTP $($response.StatusCode))."
            }
        }
    }

    $download = (New-TemporaryFile).FullName
    try {
        $uri = Get-PackageUri $metadata.id $metadata.version
        $response = Invoke-WebRequest -Uri $uri -OutFile $download -PassThru -SkipHttpErrorCheck
        if ($response.StatusCode -eq 404) { return $false }
        if ($response.StatusCode -ne 200) { throw "Cannot check $uri (HTTP $($response.StatusCode))." }

        $published = [IO.Compression.ZipFile]::OpenRead($download)
        try { $actual = Get-PackageContents $published } finally { $published.Dispose() }
        $expected = Get-PackageContents $local
        if ($expected.Count -ne $actual.Count) { throw "Published package has different entries from $PackagePath." }
        foreach ($name in $expected.Keys) {
            if (!$actual.ContainsKey($name) -or $actual[$name] -cne $expected[$name]) {
                throw "Published package differs from $PackagePath at '$name'."
            }
        }
        Write-Host "NuGet.org already contains the validated contents of $($metadata.id) $($metadata.version)."
        return $true
    } finally { Remove-Item -LiteralPath $download }
} finally { $local.Dispose() }
