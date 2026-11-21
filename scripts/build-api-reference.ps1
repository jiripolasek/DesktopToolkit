[CmdletBinding()]
param(
  [string]$ComponentsRoot = $env:COMPONENTS_ROOT
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
$site = Split-Path -Parent $PSScriptRoot
if (!$ComponentsRoot) {
  throw 'Set COMPONENTS_ROOT or pass -ComponentsRoot with the path to the library checkout.'
}
$library = (Resolve-Path -LiteralPath $ComponentsRoot).Path
$reference = Join-Path $site '.api-reference'
$manifest = Join-Path $reference 'components.json'
$components = & (Join-Path $library 'eng/Get-Components.ps1')

Push-Location $site
try {
  dotnet tool restore
  if (Test-Path -LiteralPath $manifest -PathType Leaf) { Remove-Item -LiteralPath $manifest }
  $generated = @(foreach ($component in $components) {
    Push-Location $library
    try {
      $project = $component.project
      $properties = dotnet msbuild $project -nologo '-getProperty:GenerateDocumentationFile,TargetFramework,TargetFrameworks,AssemblyName' |
        ConvertFrom-Json | Select-Object -ExpandProperty Properties
      if ($properties.GenerateDocumentationFile -ne 'true') { continue }

      $framework = $properties.TargetFramework
      if (!$framework) { $framework = ($properties.TargetFrameworks -split ';')[0] }
      $build = Join-Path $reference "bin/$($component.slug)"
      dotnet build $project -c Release -f $framework -p:Platform=x64 -p:CopyLocalLockFileAssemblies=true --output $build | Out-Host
    } finally { Pop-Location }

    $assembly = Join-Path $build "$($properties.AssemblyName).dll"
    $output = Join-Path $reference "markdown/$($component.slug)"
    $links = Join-Path $output 'links.txt'
    dotnet tool run defaultdocumentation -- -j "$PSScriptRoot/api-reference.json" -a $assembly -o $output -l $links | Out-Host
    if (!(Test-Path -LiteralPath (Join-Path $output 'index.md') -PathType Leaf)) {
      throw "No API reference was generated for $($component.packageId)."
    }
    if (!(Test-Path -LiteralPath $links -PathType Leaf)) {
      throw "No API link map was generated for $($component.packageId)."
    }
    $component.slug
  })
  if (!$generated.Count) { throw 'No component projects generated API documentation.' }
  ConvertTo-Json -InputObject $generated | Set-Content -LiteralPath $manifest -Encoding utf8
} finally { Pop-Location }
