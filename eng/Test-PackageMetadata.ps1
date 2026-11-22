#requires -Version 7.0

[CmdletBinding()]
param(
    [string]$Tag,
    [string]$PackageDirectory = (Join-Path $PSScriptRoot '../artifacts/packages')
)

$ErrorActionPreference = 'Stop'
$components = & "$PSScriptRoot/Get-Components.ps1" -Tag $Tag
foreach ($component in $components) {
    $package = "$($component.packageId).$($component.version)"
    $packagePath = Join-Path $PackageDirectory "$package.nupkg"
    if (!(Test-Path -LiteralPath $packagePath -PathType Leaf)) {
        throw "Missing release artifact $packagePath."
    }
    $symbolPath = [IO.Path]::ChangeExtension($packagePath, 'snupkg')
    if ($component.hasSymbols -and !(Test-Path -LiteralPath $symbolPath -PathType Leaf)) {
        throw "Missing release artifact $symbolPath."
    }

    $archive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $packagePath).Path)
    try {
        $manifest = $archive.GetEntry("$($component.packageId).nuspec")
        if (!$manifest) { throw "$package has no NuGet manifest." }
        $stream = $manifest.Open()
        try {
            $document = [xml]::new()
            $document.Load($stream)
        } finally { $stream.Dispose() }

        $metadata = $document.package.metadata
        $expected = @{
            id = $component.packageId
            version = $component.version
            title = $component.title
            description = $component.description
            projectUrl = "https://desktoptoolkit.jiripolasek.com/components/$($component.slug)/"
        }
        foreach ($field in $expected.Keys) {
            if ($metadata.$field -cne $expected[$field]) {
                throw "$package metadata '$field' must be '$($expected[$field])'."
            }
        }
        foreach ($field in 'authors', 'tags') {
            if ([string]::IsNullOrWhiteSpace($metadata.$field)) { throw "$package is missing '$field'." }
        }
        if ([string]::IsNullOrWhiteSpace($metadata.repository.url) -or $metadata.repository.type -ne 'git') {
            throw "$package is missing Git repository metadata."
        }

        foreach ($field in 'icon', 'readme') {
            $path = [string]$metadata.$field
            if (!$path -or !$archive.GetEntry($path).Length) {
                throw "$package is missing the embedded '$field' file."
            }
        }
        if ($metadata.license.type -cne 'expression' -or $metadata.license.InnerText -cne 'Apache-2.0') {
            throw "$package must declare the Apache-2.0 license."
        }
        if (!$archive.GetEntry('LICENSE').Length) {
            throw "$package is missing the bundled LICENSE file."
        }
        Write-Host "Checked NuGet metadata and embedded assets for $package."
    } finally { $archive.Dispose() }
}
