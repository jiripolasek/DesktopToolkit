param(
    [Parameter(Mandatory)][ValidateSet('sandbox', 'local')][string]$Target,
    [Parameter(Mandatory)][int]$AppPid,
    [Parameter(Mandatory)][ValidateSet('sandbox', 'local')][string]$AppScope,
    [string[]]$CaseFilter = @('*'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '../artifacts')
)
$ErrorActionPreference = 'Stop'
if ($AppPid -le 0 -or $AppScope -ne $Target) { throw 'A live PID and matching scope are required.' }
$scopeArgs = @(if ($Target -eq 'sandbox') { '--on'; 'sandbox' })
$previousWorkflow = $env:WINAPP_UI_WORKFLOW_ID
$env:WINAPP_UI_WORKFLOW_ID = [guid]::NewGuid().ToString('N')
$artifactDirectory = Join-Path ([IO.Path]::GetFullPath($OutputDirectory, $PWD.ProviderPath)) ('native-search-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $artifactDirectory -Force | Out-Null
$results = [Collections.Generic.List[object]]::new()
function Ui([string[]]$Arguments) {
    $output = & winapp ui @Arguments @scopeArgs -a $AppPid
    if ($LASTEXITCODE -ne 0) { throw ($output -join [Environment]::NewLine) }
    $output
}
function Keys([string]$Text) { Ui @('send-keys', $Text, '--via', 'send-input') | Out-Null }
function Open-Native {
    Keys 'esc esc esc'
    Ui @('wait-for', 'NativeSearchFilterBox', '--gone', '-t', '2500') | Out-Null
    Ui @('invoke', 'SearchableNativeMenuButton') | Out-Null
    Ui @('wait-for', 'NativeSearchFilterBox', '-p', 'HasKeyboardFocus', '--value', 'True', '-t', '2500') | Out-Null
}
function Set-Query([string]$Text, [int]$Count, [string]$Suffix = '') {
    Ui @('set-value', "NativeSearchFilterBox$Suffix", $Text) | Out-Null
    Ui @('wait-for', "NativeSearchPresenter$Suffix", '-p', 'HelpText', '--value', "$Count matching", '--contains', '-t', '2500') | Out-Null
}
function Bounds([string]$Id) {
    $rect = Ui @('get-property', $Id, '-p', 'BoundingRectangle', '--json') | ConvertFrom-Json
    ,[double[]]($rect.properties.BoundingRectangle -split ',')
}
function Assert-Pinned([double[]]$Before, [string]$Suffix = '') {
    $after = Bounds "NativeSearchFilterBox$Suffix"
    if ([math]::Abs($Before[0]-$after[0]) -gt 1 -or [math]::Abs($Before[1]-$after[1]) -gt 1) {
        throw "Filter moved: $($Before -join ',') -> $($after -join ',')."
    }
}
function Assert-MenuPlacement([double[]]$Unfiltered, [bool]$OpensUpward) {
    $filtered = Bounds 'NativeSearchPresenter'
    if ($OpensUpward) {
        for ($index = 0; $index -lt 4; $index++) {
            if ([math]::Abs($Unfiltered[$index]-$filtered[$index]) -gt 1) {
                throw "The upward popup window moved: $($Unfiltered -join ',') -> $($filtered -join ',')."
            }
        }
    }
    elseif ($filtered[3] -ge $Unfiltered[3] - 1 -or [math]::Abs($filtered[2]-$Unfiltered[2]) -gt 1) {
        throw "Expected a shorter menu with the same width: $($Unfiltered -join ',') -> $($filtered -join ',')."
    }
}
function Test-Ui([string]$Name, [scriptblock]$Body) {
    if (-not ($CaseFilter | Where-Object { $Name -like $_ })) { return }
    try { & $Body | Out-Null; $results.Add(@{ name=$Name; status='PASS' }); "PASS: $Name" }
    catch { $results.Add(@{ name=$Name; status='FAIL'; detail="$_" }); "FAIL: $Name : $_" }
}
function Screenshot([string]$Name) {
    Ui @('screenshot', '-w', $mainHwnd, '--capture-screen', '-o', (Join-Path $artifactDirectory $Name)) | Out-Null
}
try {
    $main = @(Ui @('list-windows', '--json') | ConvertFrom-Json) | Where-Object title -eq 'Filterable context menu' | Select-Object -First 1
    if (-not $main.hwnd) { throw 'Expected app window missing.' }
    $mainHwnd = [string]$main.hwnd
    Ui @('focus', '-w', $mainHwnd) | Out-Null
    Test-Ui 'Native search stays pinned as the menu filters' {
        Open-Native
        $before = Bounds 'NativeSearchFilterBox'
        $presenter = Bounds 'NativeSearchPresenter'
        $opensUpward = $before[1] - $presenter[1] -gt 4
        $unfiltered = (Ui @('get-property', 'NativeSearchPresenter', '-p', 'HelpText', '--json') | ConvertFrom-Json).properties.HelpText
        $inspection = Ui @('inspect', '--depth', '1', '--json') | ConvertFrom-Json
        $maxInset = 2 * $inspection.windows[0].windowDpi / 96.0 + 1
        $insets = @(
            $before[0] - $presenter[0]
            $presenter[0] + $presenter[2] - $before[0] - $before[2]
        )
        if ($insets | Where-Object { $_ -lt -1 -or $_ -gt $maxInset }) {
            throw 'The filter must span the full menu interior.'
        }
        Screenshot '01-native-full.png'
        Set-Query 'SAVE' 1
        Assert-Pinned $before
        Assert-MenuPlacement $presenter $opensUpward
        Set-Query 'zz-no-match' 0
        Ui @('wait-for', 'NativeSearchEmptyMessage', '--value', 'No matching commands', '-t', '2500')
        Assert-Pinned $before
        Assert-MenuPlacement $presenter $opensUpward
        Keys 'enter'
        Ui @('wait-for', 'NativeSearchFilterBox', '-t', '2500')
        Screenshot '02-native-empty.png'
        Keys 'ctrl+a backspace'
        Ui @('wait-for', 'NativeSearchPresenter', '-p', 'HelpText', '--value', $unfiltered, '-t', '2500')
        Assert-Pinned $before
        $restored = Bounds 'NativeSearchPresenter'
        if ([math]::Abs($presenter[3]-$restored[3]) -gt 1) { throw 'Clearing the filter did not restore the height.' }
    }
    Test-Ui 'Native Space edits the filter and activates focused commands' {
        Open-Native
        Keys 'export space as'
        Ui @('wait-for', 'NativeSearchFilterBox', '--value', 'export as', '-t', '2500')
        Ui @('wait-for', 'NativeSearchPresenter', '-p', 'HelpText', '--value', '1 matching', '--contains', '-t', '2500')
        Set-Query 'write' 1
        Keys 'down space'
        Ui @('wait-for', 'LastActionText', '--value', 'Native searchable: Save.', '-t', '2500')
    }
    Test-Ui 'Native Click handler runs from search Enter' {
        Open-Native
        Set-Query 'write' 1
        Keys 'enter'
        Ui @('wait-for', 'LastActionText', '--value', 'Native searchable: Save.', '-t', '2500')
    }
    Test-Ui 'Native disabled command remains disabled' {
        Open-Native
        Set-Query 'export selection' 1
        Ui @('wait-for', 'Export selection', '--type', 'MenuItem', '-p', 'IsEnabled', '--value', 'False', '-t', '2500')
        Keys 'enter'
        Ui @('wait-for', 'NativeSearchFilterBox', '-t', '2500')
    }
    Test-Ui 'Native result typing editing and arrow boundaries use search' {
        Open-Native
        Set-Query 'sa' 1
        Keys 'end down'
        Ui @('wait-for', 'Save', '--type', 'MenuItem', '-p', 'HasKeyboardFocus', '--value', 'True', '-t', '2500')
        Keys 've'
        Ui @('wait-for', 'NativeSearchFilterBox', '--value', 'save', '-t', '2500')
        Keys 'down backspace'
        Ui @('wait-for', 'NativeSearchFilterBox', '--value', 'sav', '-t', '2500')
        Keys 'down down'
        Ui @('wait-for', 'NativeSearchFilterBox', '-p', 'HasKeyboardFocus', '--value', 'True', '-t', '2500')
        Keys 'up up'
        Ui @('wait-for', 'NativeSearchFilterBox', '-p', 'HasKeyboardFocus', '--value', 'True', '-t', '2500')
    }
    Test-Ui 'Native toggle keeps its state and Click handler' {
        Open-Native
        Set-Query 'bookmark' 1
        Ui @('invoke', 'Pin to favorites', '--type', 'MenuItem', '--action', 'toggle-on')
        Open-Native
        Set-Query 'bookmark' 1
        Ui @('wait-for', 'Pin to favorites', '--type', 'MenuItem', '--value', 'On', '-t', '2500')
        Ui @('invoke', 'Pin to favorites', '--type', 'MenuItem', '--action', 'toggle-off')
        Ui @('wait-for', 'LastActionText', '--value', 'Native searchable: Pin to favorites: unchecked.', '-t', '2500')
    }
    Test-Ui 'Native submenu has an independent accessible pinned search' {
        Open-Native
        Set-Query 'export as' 1
        Keys 'down right'
        Ui @('wait-for', 'NativeSearchFilterBoxDepth1', '-p', 'HasKeyboardFocus', '--value', 'True', '-t', '2500')
        $before = Bounds 'NativeSearchFilterBoxDepth1'
        Set-Query 'portable' 1 'Depth1'
        Assert-Pinned $before 'Depth1'
        Ui @('wait-for', 'NativeSearchFilterBox', '--value', 'export as', '-t', '2500')
        Screenshot '03-native-submenu.png'
        Keys 'enter'
        Ui @('wait-for', 'LastActionText', '--value', 'Native searchable: PDF document.', '-t', '2500')
    }
    Test-Ui 'Native nested submenu search works at depth two' {
        Open-Native
        Set-Query 'export as' 1
        Keys 'down right'
        Ui @('wait-for', 'NativeSearchFilterBoxDepth1', '-t', '2500')
        Set-Query 'more' 1 'Depth1'
        Keys 'down right'
        Ui @('wait-for', 'NativeSearchFilterBoxDepth2', '-t', '2500')
        Set-Query 'webp' 1 'Depth2'
        Keys 'enter'
        Ui @('wait-for', 'LastActionText', '--value', 'Native searchable: WebP image.', '-t', '2500')
    }
    Test-Ui 'Native radio items invoke without migrating their markup' {
        Open-Native
        Set-Query 'sort' 1
        Keys 'down right'
        Ui @('wait-for', 'NativeSearchFilterBoxDepth1', '-t', '2500')
        Set-Query 'date' 1 'Depth1'
        Keys 'enter'
        Ui @('wait-for', 'LastActionText', '--value', 'Native searchable: By date.', '-t', '2500')
    }
    Test-Ui 'Native split command invokes its original ICommand' {
        Open-Native
        Set-Query 'open recent' 1
        Keys 'enter'
        Ui @('wait-for', 'LastActionText', '--value', 'Native searchable command executed.', '-t', '2500')
    }
    Test-Ui 'Native filtering preserves a bound Visibility property' {
        Open-Native
        Set-Query 'advanced' 1
        Ui @('invoke', 'Show advanced commands', '--type', 'MenuItem', '--action', 'toggle-on')
        Open-Native
        Set-Query 'advanced' 2
        Set-Query 'save' 1
        Open-Native
        Set-Query 'advanced' 2
        Ui @('invoke', 'Show advanced commands', '--type', 'MenuItem', '--action', 'toggle-off')
        Open-Native
        Set-Query 'advanced' 1
    }
    Test-Ui 'Native keyboard accelerator still invokes the native item' {
        Open-Native
        Keys 'ctrl+shift+n'
        Ui @('wait-for', 'LastActionText', '--value', 'Native searchable: New file.', '-t', '2500')
    }
    Test-Ui 'Native accelerator Invoked handler can replace the default action' {
        Open-Native
        Set-Query 'write' 1
        Keys 'ctrl+shift+s'
        Ui @('wait-for', 'LastActionText', '--value', 'Native searchable accelerator handled.', '-t', '2500')
        Ui @('wait-for', 'NativeSearchFilterBox', '-p', 'HasKeyboardFocus', '--value', 'True', '-t', '2500')
    }
    Test-Ui 'Native scrolling keeps the filter in place' {
        Open-Native
        $before = Bounds 'NativeSearchFilterBox'
        Ui @('scroll', 'NativeSearchPresenter', '--to', 'bottom')
        Assert-Pinned $before
        Ui @('scroll', 'NativeSearchPresenter', '--to', 'top')
        Assert-Pinned $before
        Screenshot '04-native-scroll.png'
    }
    Test-Ui 'Native RTL layout keeps WinUI submenu navigation' {
        try {
            Keys 'esc esc esc'
            Ui @('invoke', 'MenuDirectionToggle', '--action', 'toggle-on')
            Open-Native
            Set-Query 'export as' 1
            # WinUI's native submenu opens with Right in either flow direction.
            Keys 'down right'
            Ui @('wait-for', 'NativeSearchFilterBoxDepth1', '-t', '2500')
            Set-Query 'picture' 1 'Depth1'
            Screenshot '05-native-rtl.png'
            Keys 'enter'
            Ui @('wait-for', 'LastActionText', '--value', 'Native searchable: PNG image.', '-t', '2500')
        } finally {
            Keys 'esc esc esc'
            Ui @('invoke', 'MenuDirectionToggle', '--action', 'toggle-off')
        }
    }
    Test-Ui 'Native light theme uses the shared field and native menu chrome' {
        try {
            Keys 'esc esc esc'
            Ui @('invoke', 'ThemePicker', '--action', 'expand')
            Ui @('set-value', 'MenuFilterBox', 'Light')
            Ui @('wait-for', 'FilteredMenuItems', '-p', 'HelpText', '--value', '1 matching', '--contains', '-t', '2500')
            Keys 'enter'
            Open-Native
            Set-Query 'export as' 1
            Keys 'down right'
            Ui @('wait-for', 'NativeSearchFilterBoxDepth1', '-t', '2500')
            Screenshot '06-native-light.png'
        } finally {
            Keys 'esc esc esc'
            Ui @('invoke', 'ThemePicker', '--action', 'expand')
            Ui @('set-value', 'MenuFilterBox', 'Dark')
            Ui @('wait-for', 'FilteredMenuItems', '-p', 'HelpText', '--value', '1 matching', '--contains', '-t', '2500')
            Keys 'enter'
        }
    }
} finally {
    try { & winapp ui yield @scopeArgs | Out-Null } finally { $env:WINAPP_UI_WORKFLOW_ID = $previousWorkflow }
}
$failed = @($results | Where-Object status -eq 'FAIL').Count
$report = @{ target=$Target; appPid=$AppPid; passed=$results.Count-$failed; failed=$failed; results=@($results.ToArray()); artifactDirectory=$artifactDirectory }
$reportJson = $report | ConvertTo-Json -Depth 5
$reportJson
try { $reportJson | Set-Content -LiteralPath (Join-Path $artifactDirectory 'results.json') -Encoding utf8 }
catch { Write-Warning "Could not save the printed report: $_" }
if ($failed) { exit 1 }
