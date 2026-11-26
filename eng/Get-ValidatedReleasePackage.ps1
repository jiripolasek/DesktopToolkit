#requires -Version 7.4

[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidatePattern('^v[0-9]')][string]$Tag,
    [Parameter(Mandatory)][ValidatePattern('^[a-f0-9]{40}$')][string]$Commit,
    [Parameter(Mandatory)][string]$Repository,
    [Parameter(Mandatory)][string]$PackageDirectory
)

$ErrorActionPreference = 'Stop'
$components = @(& "$PSScriptRoot/Get-Components.ps1" -Tag $Tag)
$actions = "repos/$Repository/actions"
$runs = gh api --paginate "$actions/workflows/ci.yml/runs?head_sha=$Commit&branch=main&status=success&per_page=100" --jq '.workflow_runs[]'
if ($LASTEXITCODE -ne 0) { throw 'Cannot list successful CI runs.' }
$artifact = $null
foreach ($run in ($runs | ConvertFrom-Json)) {
    if ($run.head_sha -ne $Commit -or $run.head_branch -ne 'main' -or $run.conclusion -ne 'success' -or
        $run.head_repository.full_name -ne $Repository -or $run.event -notin 'push', 'workflow_dispatch') {
        continue
    }
    $artifacts = gh api "$actions/runs/$($run.id)/artifacts?name=release-assets" | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw "Cannot list artifacts for CI run $($run.id)." }
    $artifact = $artifacts.artifacts | Where-Object { $_.name -ceq 'release-assets' -and !$_.expired } | Select-Object -First 1
    if ($artifact) { break }
}
if (!$artifact) {
    throw "No unexpired release-assets artifact from successful full CI on main for $Commit. Run full CI on that commit before releasing."
}
if ($artifact.digest -notmatch '^sha256:([a-f0-9]{64})$') {
    throw "GitHub did not provide a SHA-256 digest for artifact $($artifact.id)."
}
$expectedHash = $Matches[1]

Write-Host "Using release-assets artifact $($artifact.id) from CI run $($run.id) for $Commit."
$archivePath = (New-TemporaryFile).FullName
try {
    gh api "$actions/artifacts/$($artifact.id)/zip" > $archivePath
    if ($LASTEXITCODE -ne 0) { throw "Cannot download artifact $($artifact.id)." }
    if ((Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash -ne $expectedHash) {
        throw "SHA-256 digest mismatch for artifact $($artifact.id)."
    }

    New-Item -ItemType Directory -Path $PackageDirectory -Force | Out-Null
    $archive = [IO.Compression.ZipFile]::OpenRead($archivePath)
    try {
        $files = @(
            foreach ($component in $components) {
                $package = "packages/$($component.packageId).$($component.version)"
                "$package.nupkg"
                if ($component.hasSymbols) { "$package.snupkg" }
            }
            foreach ($arch in 'x64', 'arm64') {
                foreach ($suffix in '', '-single-file') {
                    "gallery/JPSoftworks.DesktopToolkit.Gallery-$($Tag.Substring(1))-win-$arch$suffix.zip"
                }
            }
        )
        foreach ($file in $files) {
            $entry = $archive.GetEntry($file)
            if (!$entry -or !$entry.Length) { throw "Artifact $($artifact.id) is missing $file." }
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Join-Path $PackageDirectory $entry.Name), $true)
        }
    } finally { $archive.Dispose() }
} finally { Remove-Item -LiteralPath $archivePath }

& "$PSScriptRoot/Test-PackageMetadata.ps1" -Tag $Tag -PackageDirectory $PackageDirectory
$components
