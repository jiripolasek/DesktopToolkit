[CmdletBinding()]
param(
    [ValidateSet('x64', 'ARM64')][string]$Platform = 'x64',
    [string]$ReleaseTag,
    [switch]$BuildOnly,
    [switch]$TestPackage
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$packages = Join-Path $root 'artifacts/packages'

function Invoke-Dotnet([string[]]$Arguments) {
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($Arguments[0]) failed with exit code $LASTEXITCODE." }
}

Push-Location $root
try {
    $components = @(& "$PSScriptRoot/Get-Components.ps1" -Tag $ReleaseTag)
    Invoke-Dotnet @('build', 'JPSoftworks.DesktopToolkit.slnx', '-c', 'Release', "-p:Platform=$Platform", '-warnaserror')
    if ($BuildOnly) { return }
    if ($components.Count -eq 1 -and $components[0].slug -ne 'core') {
        $core = & "$PSScriptRoot/Get-Components.ps1" | Where-Object slug -eq 'core'
        $assetsPath = & dotnet msbuild $components[0].project '-p:Configuration=Release' "-p:Platform=$Platform" '-getProperty:ProjectAssetsFile'
        if ($LASTEXITCODE -ne 0 -or !$assetsPath) { throw "Cannot resolve package dependencies for $($components[0].slug)." }
        $assets = Get-Content -Raw -LiteralPath $assetsPath | ConvertFrom-Json -AsHashtable
        if ($assets.libraries.ContainsKey("$($core.packageId)/$($core.version)")) { $components += $core }
    }
    foreach ($component in ($components | Sort-Object -Stable { $_.slug -ne 'core' })) {
        if ($component.testProject) {
            $frameworks = & dotnet msbuild $component.testProject '-getProperty:TargetFrameworks'
            if ($LASTEXITCODE -ne 0 -or !$frameworks) { throw "Cannot resolve test targets for $($component.slug)." }
            foreach ($framework in ($frameworks.Trim() -split ';')) {
                Invoke-Dotnet @('run', '--project', $component.testProject, '-f', $framework, '-c', 'Release', "-p:Platform=$Platform", '--no-build')
            }
        }
        Invoke-Dotnet @('pack', $component.project, '-c', 'Release', "-p:Platform=$Platform", '-p:EnableStrictModeForCompatibleTfms=true', '-warnaserror', '--no-build', '-o', $packages)
        $nupkg = Join-Path $packages "$($component.packageId).$($component.version).nupkg"
        & "$PSScriptRoot/Test-PackageMetadata.ps1" -Tag $component.tag -PackageDirectory $packages
        if ($TestPackage -and $component.packageCheck) {
            & (Join-Path $root $component.packageCheck) -PackagePath $nupkg -Architecture $Platform -ColdRestore -Flavor Aot
        }
    }
} finally { Pop-Location }
