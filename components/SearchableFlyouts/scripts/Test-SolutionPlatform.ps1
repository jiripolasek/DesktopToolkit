param(
    [ValidateSet('x64', 'ARM64')][string]$Platform = 'ARM64',
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug'
)
$ErrorActionPreference = 'Stop'
$solution = Join-Path $PSScriptRoot '../../../JPSoftworks.DesktopToolkit.slnx'
$output = @(& dotnet.exe build $solution -c $Configuration "-p:Platform=$Platform" -warnaserror -v:minimal)
$exitCode = $LASTEXITCODE
$output | Out-Host
if ($exitCode -ne 0) { throw "Solution build failed with exit code $exitCode." }

# Inspect the demo output actually reported by this build, including on incremental builds.
$line = $output | Select-String '^\s*JPSoftworks\.DesktopToolkit\.Controls\.SearchableFlyouts\.DevApp -> (.+\.dll)\s*$' | Select-Object -Last 1
if (!$line) { throw 'The solution did not report a demo assembly.' }
$directory = Split-Path -Parent $line.Matches[0].Groups[1].Value
$winUi = 'Microsoft.UI.Xaml.dll'
if (!(Test-Path -LiteralPath (Join-Path $directory $winUi) -PathType Leaf)) {
    throw "The $Platform DevApp output is missing the bundled WinUI runtime."
}
$expectedMachine = @{ x64 = 'Amd64'; ARM64 = 'Arm64' }[$Platform]
foreach ($file in 'JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts.DevApp.exe', $winUi) {
    $stream = [IO.File]::OpenRead((Join-Path $directory $file))
    try {
        $reader = [System.Reflection.PortableExecutable.PEReader]::new($stream)
        try { $machine = $reader.PEHeaders.CoffHeader.Machine } finally { $reader.Dispose() }
    } finally { $stream.Dispose() }
    if ($machine.ToString() -ine $expectedMachine) {
        throw "Requested $Platform but $file targets $machine."
    }
}
"PASS SolutionTargetsRequestedArchitecture ($Configuration $Platform)"
