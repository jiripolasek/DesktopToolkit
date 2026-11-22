#requires -Version 7.4

$ErrorActionPreference = 'Stop'
$check = Join-Path $PSScriptRoot '../Test-PackagePublication.ps1'
$manifest = @'
<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
  <metadata>
    <id>Example.Controls</id><version>0.1.0-preview.1+build</version>
    <dependencies>
      <group targetFramework="net10.0"><dependency id="Example.Leaf" version="0.1.0-preview.1" /></group>
      <group targetFramework="net11.0"><dependency id="Example.Leaf" version="0.1.0-preview.1" /></group>
    </dependencies>
  </metadata>
</package>
'@

function Write-TestPackage([string]$Path, [string]$Content, [switch]$Signature, [switch]$ExtraFile) {
    $stream = [IO.File]::Create($Path)
    $archive = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create)
    try {
        $files = [ordered]@{ 'Example.Controls.nuspec' = $manifest; 'lib/net10.0/Example.dll' = $Content }
        if ($Signature) { $files['.signature.p7s'] = 'Repository signature entry' }
        if ($ExtraFile) { $files['extra.txt'] = 'Different content' }
        foreach ($file in $files.GetEnumerator()) {
            $writer = [IO.StreamWriter]::new($archive.CreateEntry($file.Key).Open())
            try { $writer.Write($file.Value) } finally { $writer.Dispose() }
        }
    } finally {
        $archive.Dispose()
        $stream.Dispose()
    }
}

function Assert-Failure([string]$Expected, [scriptblock]$Run) {
    try { & $Run | Out-Null }
    catch {
        if ($_.Exception.Message.Contains($Expected)) { return }
        throw
    }
    throw "Expected the publication check to fail with '$Expected'."
}

# Supply the feed responses within this test's scope.
function Invoke-WebRequest([uri]$Uri, [string]$Method = 'Get', [string]$OutFile, [switch]$PassThru, [switch]$SkipHttpErrorCheck) {
    $requests.Add("$Method $($Uri.AbsolutePath)")
    if ($Method -eq 'Head') { return @{ StatusCode = $dependencyStatus } }
    if ($packageStatus -eq 200) { [IO.File]::Copy($remote, $OutFile, $true) }
    return @{ StatusCode = $packageStatus }
}

$local = (New-TemporaryFile).FullName
$remote = (New-TemporaryFile).FullName
try {
    Write-TestPackage $local 'original'
    $dependencyStatus = 200
    $packageStatus = 404
    $requests = [Collections.Generic.List[string]]::new()
    if ((& $check -PackagePath $local -CheckDependencies) -ne $false) { throw 'A new version must be published.' }
    $version = '0.1.0-preview.1'
    if ($requests.Count -ne 2 -or
        $requests[0] -cne "Head /v3-flatcontainer/example.leaf/$version/example.leaf.$version.nupkg" -or
        $requests[1] -cne "Get /v3-flatcontainer/example.controls/$version/example.controls.$version.nupkg") {
        throw "Unexpected package requests: $requests"
    }

    $dependencyStatus = 404
    Assert-Failure 'Example.Leaf' { & $check -PackagePath $local -CheckDependencies }
    $packageStatus = 200
    Write-TestPackage $remote 'original' -Signature
    if ((& $check -PackagePath $local) -ne $true) { throw 'Matching published content must be reusable.' }
    Write-TestPackage $remote 'different' -Signature
    Assert-Failure 'Example.dll' { & $check -PackagePath $local }
    Write-TestPackage $remote 'original' -Signature -ExtraFile
    Assert-Failure 'different entries' { & $check -PackagePath $local }
    $packageStatus = 503
    Assert-Failure 'HTTP 503' { & $check -PackagePath $local }
    Write-Host 'Passed package publication checks.'
} finally { Remove-Item -LiteralPath $local, $remote }
