$ErrorActionPreference = 'Stop'
$kitsRoot = (Get-ItemProperty -LiteralPath 'HKLM:\SOFTWARE\Microsoft\Windows Kits\Installed Roots' -Name KitsRoot10).KitsRoot10
$uapProps = Join-Path $kitsRoot 'DesignTime/CommonConfiguration/Neutral/UAP/10.0.19041.0/UAP.props'
if (Test-Path -LiteralPath $uapProps -PathType Leaf) { return }

$installer = Join-Path $env:RUNNER_TEMP 'winsdksetup-19041.exe'
Invoke-WebRequest -Uri 'https://go.microsoft.com/fwlink/?linkid=2120843' -OutFile $installer
$setup = Start-Process -FilePath $installer -ArgumentList '/quiet /norestart /ceip off /features OptionId.UWPManaged OptionId.UWPCPP' -PassThru -Wait -WindowStyle Hidden
if ($setup.ExitCode -notin 0, 3010 -or !(Test-Path -LiteralPath $uapProps -PathType Leaf)) {
  throw "Windows SDK 10.0.19041.0 setup failed with exit code $($setup.ExitCode)."
}
