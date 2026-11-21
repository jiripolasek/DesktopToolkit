param(
    [Parameter(Mandatory)][ValidateSet('sandbox', 'local')][string]$Target,
    [Parameter(Mandatory)][int]$AppPid,
    [Parameter(Mandatory)][ValidateSet('sandbox', 'local')][string]$AppScope,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '../artifacts')
)
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
if ($AppPid -le 0 -or $AppScope -ne $Target) { throw 'A live PID and matching scope are required.' }
$scopeArgs = @(if ($Target -eq 'sandbox') { '--on'; 'sandbox' })
$previousWorkflow = $env:WINAPP_UI_WORKFLOW_ID
$env:WINAPP_UI_WORKFLOW_ID = [guid]::NewGuid().ToString('N')
$artifactDirectory = Join-Path ([IO.Path]::GetFullPath($OutputDirectory, $PWD.ProviderPath)) ('ui-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $artifactDirectory -Force | Out-Null
$results = [Collections.Generic.List[object]]::new()
$screenshots = [Collections.Generic.List[string]]::new()
$evidence = [ordered]@{}
function Invoke-Checked {
    param([string[]]$Arguments)
    $global:LASTEXITCODE = $null
    $output = @(& winapp @Arguments)
    if ($null -eq $global:LASTEXITCODE -or $global:LASTEXITCODE -ne 0) {
        throw "winapp $($Arguments -join ' ') failed: $($output -join [Environment]::NewLine)"
    }
    $output
}
function Ui {
    param([string[]]$Arguments)
    Invoke-Checked (@('ui') + $Arguments + $scopeArgs + @('-a', "$AppPid"))
}
function Test-Ui {
    param([string]$Name, [scriptblock]$Body)
    try {
        & $Body | Out-Null
        $results.Add(@{ name = $Name; status = 'PASS' })
    } catch {
        $results.Add(@{ name = $Name; status = 'FAIL'; detail = "$_" })
    }
}
function Keys {
    param([string]$Text)
    Ui @('send-keys', $Text, '--via', 'send-input') | Out-Null
}
function Set-Query {
    param([string]$Text, [string]$Id = 'MenuFilterBox')
    Ui @('set-value', $Id, $Text) | Out-Null
}
function Wait-Count {
    param([int]$Count, [string]$Id = 'FilteredMenuItems')
    Ui @('wait-for', $Id, '-p', 'HelpText', '--value', "$Count matching", '--contains', '-t', '2500') | Out-Null
}
function Bounds {
    param([string]$Id, [string[]]$Options = @())
    $result = Ui (@('get-property', $Id, '-p', 'BoundingRectangle', '--json') + $Options) | ConvertFrom-Json
    $rect = [double[]]($result.properties.BoundingRectangle -split ',')
    if ($rect.Count -ne 4 -or $rect[2] -le 0 -or $rect[3] -le 0) { throw "No visible bounds for $Id." }
    ,$rect
}
function Get-Elements($Nodes) {
    foreach ($node in $Nodes) {
        if ($node.type) { $node }
        foreach ($name in @('windows', 'elements', 'children', 'element')) {
            if ($node.$name) { Get-Elements $node.$name }
        }
    }
}
function Assert-SubmenuOverlap {
    param([switch]$RightToLeft)
    $parentBounds = Bounds 'FilteredMenuItems'
    $childBounds = Bounds 'FilteredMenuItemsDepth1'
    $overlap = if ($RightToLeft) {
        $childBounds[0] + $childBounds[2] - $parentBounds[0]
    } else {
        $parentBounds[0] + $parentBounds[2] - $childBounds[0]
    }
    if ($overlap -le 0 -or $overlap -gt 8 * $scale) { throw 'Submenu must slightly overlap its parent.' }
}
function Assert-Pinned {
    param([double[]]$Before, [string]$Id = 'MenuFilterBox')
    $after = Bounds $Id
    if ([math]::Abs($after[0] - $Before[0]) -gt 1 -or [math]::Abs($after[1] - $Before[1]) -gt 1) {
        throw "Filter moved: $($Before -join ',') -> $($after -join ',')."
    }
}
function Open-Menu {
    Keys 'esc esc'
    Ui @('invoke', 'OpenMenuButton') | Out-Null
    Ui @('wait-for', 'MenuFilterBox', '-p', 'HasKeyboardFocus', '--value', 'True', '-t', '2500') | Out-Null
}
function Save-Screenshot {
    param([string]$Name, [string]$Window = $mainHwnd)
    $path = Join-Path $artifactDirectory $Name
    Invoke-Checked (@('ui', 'screenshot', '-w', $Window, '--capture-screen', '-o', $path) + $scopeArgs) | Out-Null
    if (-not (Test-Path -LiteralPath $path) -or (Get-Item -LiteralPath $path).Length -eq 0) { throw 'Screenshot not delivered.' }
    $screenshots.Add($path)
}
try {
    $windows = @(Ui @('list-windows', '--json') | ConvertFrom-Json)
    $main = $windows | Where-Object { $_.title -eq 'Filterable context menu' } | Select-Object -First 1
    if (-not $main.hwnd) { throw 'Expected app window not found.' }
    $mainHwnd = [string]$main.hwnd
    Ui @('focus', '-w', $mainHwnd) | Out-Null
    $inspection = Ui @('inspect', '--depth', '1', '--json') | ConvertFrom-Json
    $scale = $inspection.windows[0].windowDpi / 96.0

    Test-Ui 'Compact menu and virtualization' {
        Open-Menu
        Wait-Count 10008
        Ui @('wait-for', 'DiagnosticsText', '--value', 'matching commands.', '--contains', '-t', '2000')
        $diagnostics = Ui @('get-value', 'DiagnosticsText', '--json') | ConvertFrom-Json
        if ($diagnostics.text -notmatch '^(?<realized>.+) realized item containers for (?<total>.+) matching commands\.$') { throw 'Missing virtualization diagnostics.' }
        $realized = [int]($Matches.realized -replace '\D', '')
        $total = [int]($Matches.total -replace '\D', '')
        if ($realized -le 0 -or $realized -ge 500 -or $total -ne 10008) { throw 'Menu did not virtualize.' }
        $evidence.menuContainers = $realized
        $evidence.menuItems = $total
        $field = Bounds 'MenuFilterBox'
        $list = Bounds 'FilteredMenuItems'
        if ([math]::Abs($field[0] - $list[0]) -gt 1 -or [math]::Abs($field[2] - $list[2]) -gt 1) {
            throw 'The filter must span the full menu interior.'
        }
        $anchor = Bounds 'OpenMenuButton'
        if ($field[1] -lt $anchor[1] + $anchor[3] -or $field[1] -gt $anchor[1] + $anchor[3] + 40 * $scale) { throw 'Filter is not next to the lower anchor edge.' }
        if ($field[3] -gt 36 * $scale) { throw 'Search field is not compact.' }
        Save-Screenshot '01-compact-dark.png'
    }
    Test-Ui 'Below-anchor filter is pinned through one and zero matches' {
        $before = Bounds 'MenuFilterBox'
        $listBefore = Bounds 'FilteredMenuItems'
        Set-Query 'history 10000'
        Wait-Count 1
        Assert-Pinned $before
        $listAfter = Bounds 'FilteredMenuItems'
        if ($listAfter[3] -ge $listBefore[3]) { throw 'Results did not shrink.' }
        Save-Screenshot '02-filtered.png'
        Set-Query 'zz-no-command'
        Ui @('wait-for', 'MenuEmptyMessage', '--value', 'No matching commands', '-t', '2000')
        Assert-Pinned $before
        Keys 'enter'
        Ui @('wait-for', 'MenuFilterBox', '-t', '1000')
        Set-Query 'history 10000'
        Wait-Count 1
        Keys 'enter'
        Ui @('wait-for', 'LastActionText', '--value', 'Opened recent document 10000 through ICommand.', '-t', '2000')
    }
    Test-Ui 'Disabled command cannot execute' {
        Open-Menu
        Set-Query 'export selection'
        Wait-Count 1
        Ui @('wait-for', 'Export selection', '--type', 'MenuItem', '-p', 'IsEnabled', '--value', 'False', '-t', '2000')
        Keys 'enter'
        Ui @('wait-for', 'MenuFilterBox', '-t', '1000')
        Keys 'esc'
    }
    Test-Ui 'Keyboard navigation and invocation' {
        Open-Menu
        Set-Query 'save'
        Wait-Count 2
        Keys 'down down enter'
        Ui @('wait-for', 'LastActionText', '--value', 'Invoked Save as.', '-t', '2000')
    }
    Test-Ui 'Typing in a menu list continues the filter without losing characters' {
        Open-Menu
        Set-Query 'sa'
        Wait-Count 2
        Keys 'end down ve'
        Ui @('wait-for', 'MenuFilterBox', '--value', 'save', '-t', '2000')
        Ui @('wait-for', 'MenuFilterBox', '-p', 'HasKeyboardFocus', '--value', 'True', '-t', '2000')
        Wait-Count 2
        Keys 'down down enter'
        Ui @('wait-for', 'LastActionText', '--value', 'Invoked Save as.', '-t', '2000')
    }
    Test-Ui 'Arrow keys cycle between search and the first or last result' {
        Open-Menu
        Set-Query 'save'
        Wait-Count 2
        Keys 'end down up'
        Ui @('wait-for', 'MenuFilterBox', '-p', 'HasKeyboardFocus', '--value', 'True', '-t', '2000')
        Keys 'up'
        Ui @('wait-for', 'Save as', '--type', 'MenuItem', '-p', 'HasKeyboardFocus', '--value', 'True', '-t', '2000')
        Keys 'down'
        Ui @('wait-for', 'MenuFilterBox', '-p', 'HasKeyboardFocus', '--value', 'True', '-t', '2000')
        Keys 'esc'
        Ui @('invoke', 'LargeMenuToggle', '--action', 'toggle-off')
        Ui @('invoke', 'AboveAnchorButton')
        Set-Query 'save'
        Wait-Count 2
        Keys 'end up'
        Ui @('wait-for', 'Save as', '--type', 'MenuItem', '-p', 'HasKeyboardFocus', '--value', 'True', '-t', '2000')
        Keys 'down'
        Ui @('wait-for', 'MenuFilterBox', '-p', 'HasKeyboardFocus', '--value', 'True', '-t', '2000')
        Keys 'down up'
        Ui @('wait-for', 'MenuFilterBox', '-p', 'HasKeyboardFocus', '--value', 'True', '-t', '2000')
        Keys 'esc'
    }
    Test-Ui 'List editing keys return to search and preserve text editing' {
        Open-Menu
        Set-Query 'save'
        Wait-Count 2
        Keys 'end down backspace'
        Ui @('wait-for', 'MenuFilterBox', '--value', 'sav', '-t', '2000')
        Keys 'down ctrl+a copy'
        Ui @('wait-for', 'MenuFilterBox', '--value', 'copy', '-t', '2000')
        Wait-Count 2
        Keys 'home down delete'
        Ui @('wait-for', 'MenuFilterBox', '--value', 'opy', '-t', '2000')
        Ui @('wait-for', 'MenuFilterBox', '-p', 'HasKeyboardFocus', '--value', 'True', '-t', '2000')
        Keys 'down ctrl+f path'
        Ui @('wait-for', 'MenuFilterBox', '--value', 'path', '-t', '2000')
        Wait-Count 1
        Keys 'esc'
    }
    Test-Ui 'Checkable item retains state' {
        Open-Menu
        Set-Query 'favorites'
        Wait-Count 1
        Keys 'enter'
        Ui @('wait-for', 'LastActionText', '--value', 'Pin to favorites: checked.', '-t', '2000')
        Open-Menu
        Set-Query 'favorites'
        Wait-Count 1
        Keys 'enter'
        Ui @('wait-for', 'LastActionText', '--value', 'Pin to favorites: unchecked.', '-t', '2000')
    }
    Test-Ui 'Submenu has an independently pinned filter' {
        Open-Menu
        Set-Query 'export as'
        Wait-Count 1
        Keys 'down right'
        Ui @('wait-for', 'MenuFilterBoxDepth1', '-t', '2000')
        Assert-SubmenuOverlap
        $before = Bounds 'MenuFilterBoxDepth1'
        Save-Screenshot '03-submenu-dark.png'
        Set-Query 'vector' 'MenuFilterBoxDepth1'
        Wait-Count 1 'FilteredMenuItemsDepth1'
        Assert-Pinned $before 'MenuFilterBoxDepth1'
        Keys 'down left'
        Ui @('wait-for', 'MenuFilterBoxDepth1', '--gone', '-t', '2000')
        Keys 'right'
        Ui @('wait-for', 'MenuFilterBoxDepth1', '-t', '2000')
        Set-Query 'portable' 'MenuFilterBoxDepth1'
        Wait-Count 1 'FilteredMenuItemsDepth1'
        Keys 'enter'
        Ui @('wait-for', 'LastActionText', '--value', 'Invoked PDF document.', '-t', '2000')
        Ui @('wait-for', 'MenuFilterBox', '--gone', '-t', '2000')
    }
    Test-Ui 'Typing from a submenu result filters that submenu' {
        Open-Menu
        Set-Query 'export as'
        Wait-Count 1
        Keys 'down right'
        Ui @('wait-for', 'MenuFilterBoxDepth1', '-t', '2000')
        Keys 'down png'
        Ui @('wait-for', 'MenuFilterBoxDepth1', '--value', 'png', '-t', '2000')
        Wait-Count 1 'FilteredMenuItemsDepth1'
        Keys 'enter'
        Ui @('wait-for', 'LastActionText', '--value', 'Invoked PNG image.', '-t', '2000')
    }
    Test-Ui 'Above-anchor filter is at the bottom and remains pinned' {
        Ui @('invoke', 'LargeMenuToggle', '--action', 'toggle-off')
        Ui @('invoke', 'AboveAnchorButton')
        Ui @('wait-for', 'MenuFilterBox', '-t', '2000')
        $anchor = Bounds 'AboveAnchorButton'
        $before = Bounds 'MenuFilterBox'
        $gap = $anchor[1] - ($before[1] + $before[3])
        if ($gap -lt -1 -or $gap -gt 40 * $scale) { throw "Filter is not next to upper anchor edge: gap $gap." }
        Set-Query 'copy path'
        Wait-Count 1
        Assert-Pinned $before
        Save-Screenshot '04-above-anchor.png'
        Set-Query 'zz-no-command'
        Ui @('wait-for', 'MenuEmptyMessage', '-t', '2000')
        Assert-Pinned $before
        Keys 'esc'
    }
    Test-Ui 'ContextFlyout opens with right click' {
        Ui @('click', 'Right-click here', '--right')
        Ui @('wait-for', 'MenuFilterBox', '-t', '2000')
        Set-Query 'location'
        Wait-Count 1
        Keys 'enter'
        Ui @('wait-for', 'LastActionText', '--value', 'Invoked Copy path.', '-t', '2000')
    }
    Test-Ui 'Native and filtered ComboBoxes expose comparable popup geometry' {
        $filteredAnchor = Bounds 'SensitivityPicker'
        $nativeAnchor = Bounds 'NativeSensitivityPicker'
        Ui @('invoke', 'SensitivityPicker', '--action', 'expand')
        Ui @('wait-for', 'Medium', '--type', 'ListItem', '--root', 'FilteredMenuItems', '-t', '2000')
        $listBounds = Bounds 'FilteredMenuItems'
        $filterBounds = Bounds 'MenuFilterBox'
        $choiceOptions = @('--type', 'ListItem', '--root', 'FilteredMenuItems')
        $firstRow = Bounds 'Low' $choiceOptions
        $filteredRow = Bounds 'Medium' $choiceOptions
        $lastRow = Bounds 'High' $choiceOptions
        Save-Screenshot '17-filtered-combo-layout.png'
        Keys 'esc'
        if ([math]::Abs($filterBounds[0] - $listBounds[0]) -gt 1 -or
            [math]::Abs($filterBounds[2] - $listBounds[2]) -gt 1) {
            throw 'Filter must span the full popup interior.'
        }
        $leftInset = $firstRow[0] - $listBounds[0]
        $otherInsets = @(
            $listBounds[0] + $listBounds[2] - $firstRow[0] - $firstRow[2]
            $firstRow[1] - $listBounds[1]
            $listBounds[1] + $listBounds[3] - $lastRow[1] - $lastRow[3]
        )
        if ($otherInsets | Where-Object { [math]::Abs($_ - $leftInset) -gt 1 }) {
            throw 'Choice rows must have even insets on all four sides.'
        }
        Ui @('wait-for', 'NativeSensitivityPicker', '--value', 'Medium', '-t', '2000')
        Ui @('invoke', 'NativeSensitivityPicker', '--action', 'expand')
        Ui @('wait-for', 'Medium', '--type', 'ListItem', '-t', '2000')
        $row = Ui @('get-property', 'Medium', '--type', 'ListItem', '-p', 'BoundingRectangle', '--json') | ConvertFrom-Json
        $nativeRow = [double[]]($row.properties.BoundingRectangle -split ',')
        if ([math]::Abs($filteredRow[2] - $nativeRow[2]) -gt $scale) { throw 'Comparison popup rows have different widths.' }
        if ([math]::Abs($filteredRow[3] - $nativeRow[3]) -gt $scale) { throw 'Comparison popup rows have different heights.' }
        $evidence.comboLayout = [ordered]@{
            configuredControlWidthDip = 200
            filteredAutomationWidthDip = $filteredAnchor[2] / $scale
            nativeAutomationWidthDip = $nativeAnchor[2] / $scale
            filteredRowWidthDip = $filteredRow[2] / $scale
            nativeRowWidthDip = $nativeRow[2] / $scale
            filteredRowHeightDip = $filteredRow[3] / $scale
            nativeRowHeightDip = $nativeRow[3] / $scale
            filteredRowOffsetDip = ($filteredRow[1] - $filteredAnchor[1]) / $scale
            nativeRowOffsetDip = ($nativeRow[1] - $nativeAnchor[1]) / $scale
            filteredRowInsetDip = ($filteredRow[0] - $filteredAnchor[0]) / $scale
            nativeRowInsetDip = ($nativeRow[0] - $nativeAnchor[0]) / $scale
        }
        Save-Screenshot '18-native-combo-layout.png'
        Keys 'down enter'
        Ui @('wait-for', 'NativeSensitivityPicker', '--value', 'High', '-t', '2000')
    }
    Test-Ui 'Closed dropdown arrows cycle and wrap without opening' {
        Ui @('wait-for', 'SensitivityPicker', '--value', 'Medium', '-t', '2000')
        Ui @('focus', 'SensitivityPicker')
        $cases = @(
            @{ Key = 'down'; Choice = 'High' }
            @{ Key = 'down'; Choice = 'Low' }
            @{ Key = 'up'; Choice = 'High' }
            @{ Key = 'up'; Choice = 'Medium' }
        )
        foreach ($case in $cases) {
            Keys $case.Key
            Ui @('wait-for', 'SensitivityPicker', '--value', $case.Choice, '-t', '2000')
            Ui @('wait-for', 'SensitivityPicker', '-p', 'ExpandCollapseState', '--value', 'Collapsed', '-t', '2000')
            Ui @('wait-for', 'SelectionStatusText', '--value', "Sensitivity: $($case.Choice)", '-t', '2000')
        }
    }
    Test-Ui 'Dropdown initializes inline Items with SelectedIndex and commits a match' {
        Ui @('wait-for', 'SensitivityPicker', '--value', 'Medium', '-t', '2000')
        Ui @('invoke', 'SensitivityPicker', '--action', 'expand')
        Ui @('wait-for', 'MenuFilterBox', '-t', '2000')
        $before = Bounds 'MenuFilterBox'
        Save-Screenshot '05-dropdown-dark.png'
        Set-Query 'high'
        Wait-Count 1
        Assert-Pinned $before
        Keys 'enter'
        Ui @('wait-for', 'SensitivityPicker', '--value', 'High', '-t', '2000')
        Ui @('wait-for', 'SelectionStatusText', '--value', 'Sensitivity: High', '-t', '2000')
    }
    Test-Ui 'Dropdown F4 opens and Escape cancels' {
        Ui @('send-keys', 'f4', '--target', 'SensitivityPicker', '--via', 'send-input')
        Ui @('wait-for', 'MenuFilterBox', '-t', '2000')
        Set-Query 'low'
        Wait-Count 1
        Keys 'esc'
        Ui @('wait-for', 'SensitivityPicker', '--value', 'High', '-t', '2000')
    }
    Test-Ui 'Icon dropdown filters keywords and preserves the selected source item' {
        Ui @('wait-for', 'LibraryPicker', '--value', 'Documents', '-t', '2000')
        Ui @('invoke', 'LibraryPicker', '--action', 'expand')
        Wait-Count 4
        Save-Screenshot '08-icon-dropdown.png'
        $before = Bounds 'MenuFilterBox'
        Set-Query 'photos'
        Wait-Count 1
        Assert-Pinned $before
        Keys 'enter'
        Ui @('wait-for', 'LibraryPicker', '--value', 'Pictures', '-t', '2000')
        Ui @('wait-for', 'SelectionStatusText', '--value', 'Library: Pictures', '-t', '2000')
        Save-Screenshot '09-selected-icon.png'
        Ui @('invoke', 'LibraryPicker', '--action', 'expand')
        Set-Query 'movies'
        Wait-Count 1
        Keys 'enter'
        Ui @('wait-for', 'LibraryPicker', '--value', 'Videos', '-t', '2000')
        Ui @('wait-for', 'SelectionStatusText', '--value', 'Library: Videos', '-t', '2000')
        Save-Screenshot '11-icon-source.png'
        foreach ($choice in @('Music', 'Videos', 'Music', 'Videos')) {
            Ui @('invoke', 'LibraryPicker', '--action', 'expand')
            Set-Query $choice
            Wait-Count 1
            Keys 'enter'
            Ui @('wait-for', 'LibraryPicker', '--value', $choice, '-t', '2000')
        }
    }
    Test-Ui 'Typing from a dropdown result continues its search' {
        Ui @('invoke', 'SensitivityPicker', '--action', 'expand')
        Set-Query 'l'
        Wait-Count 1
        Keys 'end down ow'
        Ui @('wait-for', 'MenuFilterBox', '--value', 'low', '-t', '2000')
        Wait-Count 1
        Keys 'enter'
        Ui @('wait-for', 'SensitivityPicker', '--value', 'Low', '-t', '2000')
    }
    Test-Ui 'Dropdown arrows start from the current choice' {
        Ui @('wait-for', 'WorkspacePicker', '--value', 'Workspace 00042', '-t', '2000')
        $cases = @(
            @{ Key = 'down'; Choice = 'Workspace 00043' }
            @{ Key = 'up'; Choice = 'Workspace 00041' }
        )
        foreach ($case in $cases) {
            Ui @('invoke', 'WorkspacePicker', '--action', 'expand')
            try {
                Ui @('wait-for', 'MenuFilterBox', '-p', 'HasKeyboardFocus', '--value', 'True', '-t', '2000')
                Keys $case.Key
                Ui @('wait-for', $case.Choice, '--type', 'ListItem', '--root', 'FilteredMenuItems',
                    '-p', 'HasKeyboardFocus', '--value', 'True', '-t', '2000')
                Ui @('wait-for', 'WorkspacePicker', '--value', 'Workspace 00042', '-t', '2000')
            } finally {
                Keys 'esc'
            }
        }
    }
    Test-Ui 'Large dropdown virtualizes and selects the original value' {
        Ui @('invoke', 'WorkspacePicker', '--action', 'expand')
        Wait-Count 10000
        Ui @('wait-for', 'DiagnosticsText', '--value', 'matching commands.', '--contains', '-t', '2000')
        $diagnostics = Ui @('get-value', 'DiagnosticsText', '--json') | ConvertFrom-Json
        if ($diagnostics.text -notmatch '^(?<realized>.+) realized item containers for (?<total>.+) matching commands\.$') { throw 'Missing dropdown diagnostics.' }
        $realized = [int]($Matches.realized -replace '\D', '')
        $total = [int]($Matches.total -replace '\D', '')
        if ($realized -le 0 -or $realized -ge 500 -or $total -ne 10000) { throw 'Dropdown did not virtualize.' }
        $evidence.dropdownContainers = $realized
        $evidence.dropdownItems = $total
        $before = Bounds 'MenuFilterBox'
        Set-Query '10000'
        Wait-Count 1
        Assert-Pinned $before
        Keys 'enter'
        Ui @('wait-for', 'WorkspacePicker', '--value', 'Workspace 10000', '-t', '2000')
    }
    Test-Ui 'Dropdown arrows wrap between the first and last choices' {
        Ui @('wait-for', 'WorkspacePicker', '--value', 'Workspace 10000', '-t', '2000')
        Ui @('invoke', 'WorkspacePicker', '--action', 'expand')
        try {
            Ui @('wait-for', 'MenuFilterBox', '-p', 'HasKeyboardFocus', '--value', 'True', '-t', '2000')
            Keys 'down'
            Ui @('wait-for', 'Workspace 00001', '--type', 'ListItem', '--root', 'FilteredMenuItems',
                '-p', 'HasKeyboardFocus', '--value', 'True', '-t', '2000')
            Keys 'up'
            Ui @('wait-for', 'Workspace 10000', '--type', 'ListItem', '--root', 'FilteredMenuItems',
                '-p', 'HasKeyboardFocus', '--value', 'True', '-t', '2000')
            Keys 'down'
            Ui @('wait-for', 'Workspace 00001', '--type', 'ListItem', '--root', 'FilteredMenuItems',
                '-p', 'HasKeyboardFocus', '--value', 'True', '-t', '2000')
        } finally {
            Keys 'esc'
        }
        Ui @('wait-for', 'WorkspacePicker', '--value', 'Workspace 10000', '-t', '2000')
    }
    Test-Ui 'Menu automation exposes Menu and invokable MenuItem' {
        Open-Menu
        Set-Query 'save'
        Wait-Count 2
        Ui @('wait-for', 'FilteredMenuItems', '--type', 'Menu', '-t', '2000')
        Ui @('wait-for', 'Save', '--type', 'MenuItem', '-t', '2000')
        $invocation = Ui @('invoke', 'Save', '--type', 'MenuItem')
        if (($invocation -join ' ') -notmatch 'via InvokePattern') { throw 'Menu item did not expose InvokePattern.' }
        Ui @('wait-for', 'LastActionText', '--value', 'Invoked Save.', '-t', '2000')
    }
    Test-Ui 'Checkable menu item exposes TogglePattern' {
        Open-Menu
        Set-Query 'favorites'
        Wait-Count 1
        Ui @('wait-for', 'Pin to favorites', '--type', 'MenuItem', '--value', 'Off', '-t', '2000')
        Ui @('invoke', 'Pin to favorites', '--type', 'MenuItem', '--action', 'toggle-on')
        Ui @('wait-for', 'LastActionText', '--value', 'Pin to favorites: checked.', '-t', '2000')
        Open-Menu
        Set-Query 'favorites'
        Wait-Count 1
        Ui @('wait-for', 'Pin to favorites', '--type', 'MenuItem', '--value', 'On', '-t', '2000')
        Save-Screenshot '12-check-and-icon.png'
        Ui @('invoke', 'Pin to favorites', '--type', 'MenuItem', '--action', 'toggle-off')
        Ui @('wait-for', 'LastActionText', '--value', 'Pin to favorites: unchecked.', '-t', '2000')
    }
    Test-Ui 'Separators remain between groups and disappear from single-group matches' {
        Ui @('invoke', 'LargeMenuToggle', '--action', 'toggle-off')
        Open-Menu
        Wait-Count 8
        $tree = Ui @('inspect', 'FilteredMenuItems', '--depth', '3', '--json') | ConvertFrom-Json
        $separators = @(Get-Elements $tree | Where-Object { $_.type -eq 'Separator' })
        if ($separators.Count -ne 1) { throw 'Expected one group separator.' }
        Save-Screenshot '13-grouped-menu.png'
        Set-Query 'export'
        Wait-Count 2
        $tree = Ui @('inspect', 'FilteredMenuItems', '--depth', '3', '--json') | ConvertFrom-Json
        if (@(Get-Elements $tree | Where-Object { $_.type -eq 'Separator' }).Count) { throw 'Orphan separator remained after filtering.' }
        Keys 'esc'
    }
    Test-Ui 'Hover opens a submenu without stealing search focus' {
        Open-Menu
        Set-Query 'export as'
        Wait-Count 1
        Ui @('hover', 'Export as', '--type', 'MenuItem', '--dwell-time', '600')
        Ui @('wait-for', 'MenuFilterBoxDepth1', '-t', '2000')
        Ui @('wait-for', 'MenuFilterBox', '-p', 'HasKeyboardFocus', '--value', 'True', '-t', '2000')
        Ui @('invoke', 'Export as', '--type', 'MenuItem', '--action', 'expand')
        Ui @('wait-for', 'MenuFilterBoxDepth1', '-p', 'HasKeyboardFocus', '--value', 'True', '-t', '2000')
        Keys 'esc esc'
    }
    Test-Ui 'RTL submenu mirrors placement and arrow navigation' {
        try {
            Keys 'esc esc'
            Ui @('invoke', 'MenuDirectionToggle', '--action', 'toggle-on')
            Open-Menu
            Set-Query 'export as'
            Wait-Count 1
            Keys 'down left'
            Ui @('wait-for', 'MenuFilterBoxDepth1', '-p', 'HasKeyboardFocus', '--value', 'True', '-t', '2000')
            Assert-SubmenuOverlap -RightToLeft
            Save-Screenshot '14-rtl-submenu.png'
            Keys 'down right'
            Ui @('wait-for', 'MenuFilterBoxDepth1', '--gone', '-t', '2000')
            Keys 'left'
            Ui @('wait-for', 'MenuFilterBoxDepth1', '-t', '2000')
            Set-Query 'picture' 'MenuFilterBoxDepth1'
            Wait-Count 1 'FilteredMenuItemsDepth1'
            Keys 'enter'
            Ui @('wait-for', 'LastActionText', '--value', 'Invoked PNG image.', '-t', '2000')
        } finally {
            Keys 'esc esc'
            Ui @('invoke', 'MenuDirectionToggle', '--action', 'toggle-off')
        }
    }
    Test-Ui 'Built-in reference menu invokes its native Click handler' {
        Ui @('invoke', 'NativeMenuButton')
        Ui @('wait-for', 'New file', '--type', 'MenuItem', '-t', '2000')
        Save-Screenshot '10-builtin-reference.png'
        Ui @('invoke', 'New file', '--type', 'MenuItem')
        Ui @('wait-for', 'LastActionText', '--value', 'Built-in menu: New file.', '-t', '2000')
    }
    Test-Ui 'Built-in overflow menu is bounded and scrolls to its last item' {
        Ui @('invoke', 'NativeOverflowButton')
        Ui @('wait-for', 'NativeOverflowItems', '-t', '2000')
        $nativeBounds = Bounds 'NativeOverflowItems'
        if ($nativeBounds[3] -ge 250 * 32 * $scale) { throw 'Native menu was not constrained.' }
        Ui @('scroll', 'NativeOverflowItems', '--to', 'bottom')
        Ui @('wait-for', 'Native command 250', '--type', 'MenuItem', '-p', 'IsOffscreen', '--value', 'False', '-t', '2000')
        Ui @('wait-for', 'DiagnosticsText', '--value', 'Native menu: 250 items, 250 realized menu controls.', '-t', '2000')
        $popup = @(Ui @('list-windows', '--json') | ConvertFrom-Json) |
            Where-Object { $_.title -eq 'PopupHost' -and $_.ownerHwnd -eq [long]$mainHwnd } | Select-Object -First 1
        if (-not $popup.hwnd) { throw 'Native overflow popup was not found.' }
        Save-Screenshot '16-native-overflow.png' ([string]$popup.hwnd)
        $evidence.nativeOverflowItems = 250
        $evidence.nativeOverflowContainers = 250
        $evidence.nativeOverflowHeight = $nativeBounds[3]
        Ui @('invoke', 'Native command 250', '--type', 'MenuItem')
        Ui @('wait-for', 'LastActionText', '--value', 'Built-in menu: Native command 250.', '-t', '2000')
    }
    Test-Ui 'Light theme menu and dropdown render' {
        Ui @('invoke', 'ThemePicker', '--action', 'expand')
        Set-Query 'Light'
        Wait-Count 1
        Keys 'enter'
        Open-Menu
        Set-Query 'export as'
        Wait-Count 1
        Keys 'down right'
        Ui @('wait-for', 'MenuFilterBoxDepth1', '-t', '2000')
        Save-Screenshot '06-submenu-light.png'
        Keys 'esc esc'
        Ui @('invoke', 'SensitivityPicker', '--action', 'expand')
        Ui @('wait-for', 'MenuFilterBox', '-t', '2000')
        Save-Screenshot '07-dropdown-light.png'
        Keys 'esc'
    }
    Test-Ui 'Restore dark theme and large demo menu' {
        Ui @('invoke', 'ThemePicker', '--action', 'expand')
        Set-Query 'Dark'
        Wait-Count 1
        Keys 'enter'
        Ui @('invoke', 'LargeMenuToggle', '--action', 'toggle-on')
    }
} catch {
    $results.Add(@{ name = 'Target setup'; status = 'FAIL'; detail = "$_" })
} finally {
    try { Invoke-Checked (@('ui', 'yield') + $scopeArgs) | Out-Null }
    finally { $env:WINAPP_UI_WORKFLOW_ID = $previousWorkflow }
}
$failed = @($results | Where-Object { $_.status -eq 'FAIL' }).Count
$report = [ordered]@{ target=$Target; appPid=$AppPid; passed=$results.Count-$failed; failed=$failed; results=@($results.ToArray()); evidence=$evidence; screenshots=@($screenshots.ToArray()) }
$reportPath = Join-Path $artifactDirectory 'results.json'
$reportJson = $report | ConvertTo-Json -Depth 6
Write-Output $reportPath
Write-Output $reportJson
try { $reportJson | Set-Content -LiteralPath $reportPath -Encoding utf8 }
catch { Write-Warning "The report is printed above; could not save it: $_" }
if ($failed) { exit 1 }
