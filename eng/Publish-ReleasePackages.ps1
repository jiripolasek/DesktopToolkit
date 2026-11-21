#requires -Version 7.4

[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidatePattern('^v[0-9]')][string]$Tag,
    [Parameter(Mandatory)][string]$PackageDirectory
)

$ErrorActionPreference = 'Stop'
if (!$env:NUGET_API_KEY) { throw 'NuGet login must supply the publishing credential.' }
$source = 'https://api.nuget.org/v3/index.json'
$components = & "$PSScriptRoot/Get-Components.ps1" -Tag $Tag

# Check the entire release for missing files and version conflicts before the first upload.
$plan = foreach ($component in $components) {
    $name = "$($component.packageId).$($component.version)"
    $package = Join-Path $PackageDirectory "$name.nupkg"
    $symbols = if ($component.hasSymbols) { Join-Path $PackageDirectory "$name.snupkg" }
    if (!(Test-Path -LiteralPath $package -PathType Leaf) -or ($symbols -and !(Test-Path -LiteralPath $symbols -PathType Leaf))) {
        throw "Missing release files for $name."
    }
    [pscustomobject]@{
        Package = $package
        Symbols = $symbols
        IsBundle = $component.slug -eq 'controls'
        Published = & "$PSScriptRoot/Test-PackagePublication.ps1" -PackagePath $package
    }
}

foreach ($item in ($plan | Sort-Object IsBundle, Package)) {
    if ($item.IsBundle) {
        $item.Published = & "$PSScriptRoot/Test-PackagePublication.ps1" -PackagePath $item.Package -CheckDependencies -DependencyWaitSeconds 1200
    }
    if (!$item.Published) {
        & dotnet nuget push $item.Package --source $source --api-key $env:NUGET_API_KEY --no-symbols
        if ($LASTEXITCODE -ne 0) { throw "NuGet package push failed for $($item.Package)." }
    }
    if ($item.Symbols) {
        & dotnet nuget push $item.Symbols --source $source --api-key $env:NUGET_API_KEY --skip-duplicate
        if ($LASTEXITCODE -ne 0) { throw "NuGet symbol push failed for $($item.Symbols)." }
    }
}
