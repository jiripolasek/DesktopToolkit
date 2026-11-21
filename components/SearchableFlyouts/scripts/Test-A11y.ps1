param(
    [Parameter(Mandatory)][ValidateSet('sandbox', 'local')][string]$Target,
    [Parameter(Mandatory)][int]$AppPid,
    [Parameter(Mandatory)][ValidateSet('sandbox', 'local')][string]$AppScope,
    [string[]]$CaseFilter = @('*'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '../artifacts')
)
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
if ($AppPid -le 0 -or $AppScope -ne $Target) { throw 'A live PID and matching scope are required.' }
$scopeArgs = @(if ($Target -eq 'sandbox') { '--on'; 'sandbox' })
$previousWorkflow = $env:WINAPP_UI_WORKFLOW_ID
$env:WINAPP_UI_WORKFLOW_ID = [guid]::NewGuid().ToString('N')
$artifactDirectory = Join-Path ([IO.Path]::GetFullPath($OutputDirectory, $PWD.ProviderPath)) ('a11y-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $artifactDirectory -Force | Out-Null
$results = [Collections.Generic.List[object]]::new()
$screenshots = [Collections.Generic.List[string]]::new()
function Ui([string[]]$Arguments) {
    $output = @(& winapp ui @Arguments @scopeArgs -a $AppPid)
    if ($LASTEXITCODE -ne 0) { throw ($output -join [Environment]::NewLine) }
    $output
}
function Keys([string]$Text) { Ui @('send-keys', $Text, '--via', 'send-input') | Out-Null }
function Check([string]$Id, [string]$Property, [string]$Value) {
    Ui @('wait-for', $Id, '-p', $Property, '--value', $Value, '-t', '2500') | Out-Null
}
function Test-Ui([string]$Name, [scriptblock]$Body) {
    if (-not ($CaseFilter | Where-Object { $Name -like $_ })) { return }
    try { & $Body | Out-Null; $results.Add(@{name=$Name;status='PASS'}); "PASS: $Name" }
    catch { $results.Add(@{name=$Name;status='FAIL';detail="$_"}); "FAIL: $Name : $_" }
}
function Open-Virtual {
    Keys 'esc esc esc'
    Ui @('focus', 'AccessibleMenuButton') | Out-Null
    Ui @('invoke', 'AccessibleMenuButton') | Out-Null
    Check 'MenuFilterBox' 'HasKeyboardFocus' 'True'
}
function Open-Native {
    Keys 'esc esc esc'
    Ui @('focus', 'AccessibleNativeButton') | Out-Null
    Ui @('invoke', 'AccessibleNativeButton') | Out-Null
    Check 'NativeSearchFilterBox' 'HasKeyboardFocus' 'True'
}
function Screenshot([string]$Name) {
    $file = Join-Path $artifactDirectory $Name
    Ui @('screenshot', '-w', $mainHwnd, '--capture-screen', '-o', $file) | Out-Null
    if (-not (Test-Path -LiteralPath $file)) { throw 'Screenshot missing.' }
    $screenshots.Add($file)
}
try {
    $main = @(Ui @('list-windows', '--json') | ConvertFrom-Json) | Where-Object title -eq 'Filterable context menu' | Select-Object -First 1
    if (-not $main.hwnd) { throw 'Expected app window missing.' }
    $mainHwnd = [string]$main.hwnd
    Ui @('focus', '-w', $mainHwnd) | Out-Null
    Keys 'esc esc esc'
    Ui @('scroll-into-view', 'AccessibilitySamples') | Out-Null
    Ui @('invoke', 'AccessibilitySamples', '--action', 'expand') | Out-Null
    Ui @('scroll-into-view', 'RenameDestinationButton') | Out-Null
    Test-Ui 'Dropdown honors an external accessible label and read-only value' {
        Check 'LiveChoicesPicker' 'Name' 'Destination'
        Check 'LiveChoicesPicker' 'ControlType' 'ComboBox'
        Check 'LiveChoicesPicker' 'IsReadOnly' 'True'
        Ui @('wait-for', 'LiveChoicesPicker', '--value', 'Primary display', '-t', '2500')
    }
    Test-Ui 'Dropdown exposes expansion and accessible choice descriptions' {
        Ui @('focus', 'LiveChoicesPicker')
        Ui @('invoke', 'LiveChoicesPicker', '--action', 'expand')
        Check 'LiveChoicesPicker' 'ExpandCollapseState' 'Expanded'
        Check 'PrimaryDisplayChoice' 'Name' 'Primary display destination'
        Check 'PrimaryDisplayChoice' 'HelpText' 'Receives new windows.'
        Keys 'esc'
        Check 'LiveChoicesPicker' 'ExpandCollapseState' 'Collapsed'
        Check 'LiveChoicesPicker' 'HasKeyboardFocus' 'True'
    }
    Test-Ui 'Live item label changes update the selected value and filter' {
        Keys 'esc esc'
        Ui @('invoke', 'RenameDestinationButton')
        Ui @('wait-for', 'LiveChoicesPicker', '--value', 'Work display', '-t', '2500')
        Ui @('invoke', 'LiveChoicesPicker', '--action', 'expand')
        Ui @('set-value', 'MenuFilterBox', 'work')
        Check 'FilteredMenuItems' 'HelpText' '1 matching choice'
        Check 'PrimaryDisplayChoice' 'Name' 'Primary display destination'
        Keys 'esc'
        Ui @('invoke', 'RenameDestinationButton')
        Ui @('wait-for', 'LiveChoicesPicker', '--value', 'Primary display', '-t', '2500')
    }
    Test-Ui 'Virtualized menu exposes custom names descriptions and a menu role' {
        Open-Virtual
        Check 'FilteredMenuItems' 'ControlType' 'Menu'
        Check 'FilteredMenuItems' 'Name' 'Available operations'
        Check 'InspectDestination' 'Name' 'Inspect selected destination'
        Check 'InspectDestination' 'HelpText' 'Shows details for the selected destination.'
        Screenshot '01-virtual-accessibility.png'
    }
    Test-Ui 'Virtualized search exposes custom keyboard help and current results' {
        Open-Virtual
        Check 'MenuFilterBox' 'Name' 'Find an operation'
        Ui @('set-value', 'MenuFilterBox', 'inspect')
        Ui @('wait-for', 'MenuFilterBox', '-p', 'HelpText', '--value', '1 result available', '--contains', '-t', '2500')
        Ui @('wait-for', 'MenuFilterBox', '-p', 'HelpText', '--value', 'Use the arrow keys', '--contains', '-t', '2500')
    }
    Test-Ui 'Virtualized empty results stay keyboard reachable and describe the empty state' {
        Open-Virtual
        Ui @('set-value', 'MenuFilterBox', 'zz-no-match')
        Ui @('wait-for', 'MenuFilterBox', '-p', 'HelpText', '--value', 'No operations found', '--contains', '-t', '2500')
        Keys 'down up enter'
        Check 'MenuFilterBox' 'HasKeyboardFocus' 'True'
        Keys 'esc'
        Check 'AccessibleMenuButton' 'HasKeyboardFocus' 'True'
    }
    Test-Ui 'Virtualized checkable items expose the persisted toggle state' {
        Open-Virtual
        Ui @('invoke', 'FavoriteDestination', '--action', 'toggle-on')
        Open-Virtual
        Ui @('wait-for', 'FavoriteDestination', '--value', 'On', '-t', '2500')
        Ui @('invoke', 'FavoriteDestination', '--action', 'toggle-off')
    }
    Test-Ui 'Virtualized submenu search inherits custom messages and returns focus' {
        Open-Virtual
        Ui @('invoke', 'MoreDestination', '--action', 'expand')
        Check 'MenuFilterBoxDepth1' 'Name' 'Find in More'
        Keys 'esc'
        Check 'MoreDestination' 'HasKeyboardFocus' 'True'
        Keys 'esc'
        Check 'AccessibleMenuButton' 'HasKeyboardFocus' 'True'
    }
    Test-Ui 'Native search uses the custom accessible label and result messages' {
        Open-Native
        Check 'NativeSearchFilterBox' 'Name' 'Find a native operation'
        Check 'NativeSearchPresenter' 'Name' 'Available operations'
        Ui @('set-value', 'NativeSearchFilterBox', 'inspect')
        Ui @('wait-for', 'NativeSearchFilterBox', '-p', 'HelpText', '--value', '1 result available', '--contains', '-t', '2500')
        Screenshot '02-native-accessibility.png'
    }
    Test-Ui 'Native empty results remain focused and Escape restores the anchor' {
        Open-Native
        Ui @('set-value', 'NativeSearchFilterBox', 'zz-no-match')
        Ui @('wait-for', 'NativeSearchFilterBox', '-p', 'HelpText', '--value', 'No operations found', '--contains', '-t', '2500')
        Keys 'down up enter'
        Check 'NativeSearchFilterBox' 'HasKeyboardFocus' 'True'
        Keys 'esc'
        Check 'AccessibleNativeButton' 'HasKeyboardFocus' 'True'
    }
} catch {
    $results.Add(@{name='Target setup';status='FAIL';detail="$_"})
} finally {
    try {
        Keys 'esc esc esc'
        Ui @('invoke', 'AccessibilitySamples', '--action', 'collapse') | Out-Null
        Ui @('scroll-into-view', 'OpenMenuButton') | Out-Null
    } catch { Write-Warning "Could not reset demo: $_" }
    try { & winapp ui yield @scopeArgs | Out-Null } finally { $env:WINAPP_UI_WORKFLOW_ID = $previousWorkflow }
}
$failed = @($results | Where-Object status -eq 'FAIL').Count
$report = [ordered]@{target=$Target;appPid=$AppPid;passed=$results.Count-$failed;failed=$failed;results=@($results.ToArray());screenshots=@($screenshots.ToArray());artifactDirectory=$artifactDirectory}
$reportJson = $report | ConvertTo-Json -Depth 6
$reportJson
try { $reportJson | Set-Content -LiteralPath (Join-Path $artifactDirectory 'results.json') -Encoding utf8 }
catch { Write-Warning "The report is printed above; could not save it: $_" }
if ($failed) { exit 1 }
