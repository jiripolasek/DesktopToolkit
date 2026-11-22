[CmdletBinding()]
param([string]$Tag, [switch]$Json)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$catalog = @(foreach ($dir in Get-ChildItem -Directory -LiteralPath (Join-Path $root 'components') | Sort-Object Name) {
    $manifest = Join-Path $dir.FullName 'component.props'
    $values = ([xml](Get-Content -Raw -LiteralPath $manifest)).Project.PropertyGroup
    $slug = [string]$values.ComponentSlug
    $id = [string]$values.PackageId
    $version = [string]$values.Version
    if ($slug -cnotmatch '^[a-z0-9]+(-[a-z0-9]+)*$' -or $id -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]*$' -or
        $version -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$' -or !$values.Title -or !$values.Description -or !$values.ComponentStage) {
        throw "Invalid package metadata in $manifest."
    }
    $base = "components/$($dir.Name)"
    $project = "$base/src/$id.csproj"
    $test = if ($values.ComponentTestProject) { "$base/$($values.ComponentTestProject)" }
    $check = if ($values.ComponentPackageCheck) { "$base/$($values.ComponentPackageCheck)" }
    foreach ($path in @($project, $test, $check) | Where-Object { $_ }) {
        if (!(Test-Path -LiteralPath (Join-Path $root $path) -PathType Leaf)) { throw "Missing $path." }
    }
    [pscustomobject]@{
        slug = $slug; title = [string]$values.Title; stage = [string]$values.ComponentStage
        packageId = $id; version = $version; description = [string]$values.Description
        project = $project; testProject = $test; packageCheck = $check
        hasSymbols = [string]$values.IncludeSymbols -ne 'false'
        tag = "$slug/v$version"
    }
})
if (!$catalog.Count) { throw 'The component catalog is empty.' }
foreach ($key in 'slug', 'packageId') {
    if ($catalog | Group-Object $key | Where-Object Count -gt 1) { throw "Duplicate component $key." }
}
if ($Tag -clike 'v*') {
    $mismatches = @($catalog | Where-Object { "v$($_.version)" -cne $Tag })
    if ($mismatches.Count) {
        throw "Toolkit tag '$Tag' must match every package version. Check: $($mismatches.packageId -join ', ')."
    }
} elseif ($Tag) {
    $catalog = @($catalog | Where-Object { $_.tag -ceq $Tag })
    if ($catalog.Count -ne 1) { throw "No component matches '$Tag'." }
}
if ($Json) { ConvertTo-Json -InputObject $catalog -Depth 3 } else { $catalog }
