<#
.SYNOPSIS
    Formats C# and XAML, including ReSharper file layout.
.PARAMETER Include
    Limits formatting to these repository-relative or absolute files.
.EXAMPLE
    .\eng\Format.ps1
#>
param(
    [AllowEmptyCollection()]
    [string[]] $Include
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$limited = $PSBoundParameters.ContainsKey('Include')
$files = @()
if ($limited) {
    $files = @(foreach ($path in $Include) {
        $inputPath = if ([IO.Path]::IsPathRooted($path)) { $path } else { Join-Path $repositoryRoot $path }
        $absolute = [IO.Path]::GetFullPath($inputPath)
        if (-not $absolute.StartsWith($repositoryRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Formatting input is outside the repository: '$path'."
        }
        $relative = $absolute.Substring($repositoryRoot.Length + 1).Replace('\', '/')
        if ($relative -match '\.(cs|xaml)$' -and $relative -notmatch '(^|/)(bin|obj|artifacts|TestResults)/' -and
            [IO.File]::Exists($absolute)) {
            # These CLI options split their values, so reject names that would widen the selected scope.
            if (($relative -match '\.cs$' -and $relative.Contains(';')) -or
                ($relative -match '\.xaml$' -and $relative.Contains(','))) {
                throw "The formatter cannot select this filename literally: '$relative'."
            }
            $relative
        }
    })
    $files = @($files | Sort-Object -Unique)
    if ($files.Count -eq 0) {
        Write-Output 'No changed C# or XAML files to format.'
        return
    }
}
$csharp = @($files | Where-Object { $_ -match '\.cs$' })
$xaml = @($files | Where-Object { $_ -match '\.xaml$' })

Push-Location -LiteralPath $repositoryRoot
try {
    dotnet tool restore --tool-manifest dotnet-tools.json
    if ($LASTEXITCODE -ne 0) { throw 'Could not restore the formatting tools.' }

    if (-not $limited -or $csharp.Count -gt 0) {
        # Format linked files in their owning project so Roslyn never merges competing fixes.
        $solution = [xml](Get-Content -Raw -LiteralPath 'JPSoftworks.DesktopToolkit.slnx')
        foreach ($project in $solution.SelectNodes('//Project') | Where-Object { $_.Path -like '*.csproj' }) {
            $directories = @((Split-Path -Parent $project.Path).Replace('\', '/'))
            # Format shared localization sources through one of their consuming projects.
            if ($project.Path -eq 'components/InfoLabel/src/JPSoftworks.DesktopToolkit.Controls.InfoLabel.csproj') {
                $directories += 'shared/WinUI.Localization'
            }
            $projectFiles = @(foreach ($directory in $directories) {
                if ($limited) {
                    $csharp | Where-Object { $_.StartsWith($directory + '/', [StringComparison]::OrdinalIgnoreCase) }
                } else {
                    "$directory/**/*.cs"
                }
            })
            if ($projectFiles.Count -eq 0) { continue }
            dotnet format $project.Path --include @projectFiles
            if ($LASTEXITCODE -ne 0) { throw "dotnet format failed for '$($project.Path)'." }
        }

        # ReSharper applies file layout, qualifier arrangement and member spacing.
        $cleanupInclude = if ($limited) { $csharp -join ';' } else { 'components/**/*.cs;gallery/**/*.cs;shared/**/*.cs' }
        dotnet tool run jb -- cleanupcode JPSoftworks.DesktopToolkit.slnx --profile=DesktopToolkit --settings=eng/Format.DotSettings `
            "--include=$cleanupInclude" '--exclude=**/bin/**;**/obj/**;**/artifacts/**;**/TestResults/**' `
            --properties=Platform=x64 --no-build --no-updates --no-buildin-settings
        if ($LASTEXITCODE -ne 0) { throw 'ReSharper formatting failed.' }

        # Charts builds its package probes from temporary projects.
        $probes = @(if ($limited) {
            $csharp | Where-Object { $_ -like 'components/Charts/tests/*' } |
                ForEach-Object { Join-Path $repositoryRoot $_ }
        } else {
            Get-ChildItem -LiteralPath 'components/Charts/tests' -Filter '*.cs' -File -Recurse |
                Where-Object { $_.FullName.Substring($repositoryRoot.Length) -notmatch '[\\/](bin|obj|artifacts|TestResults)[\\/]' } |
                Select-Object -ExpandProperty FullName
        })
        if ($probes) {
            $template = Select-String -LiteralPath '.editorconfig' -Pattern '^file_header_template\s*=\s*(.+)$'
            $header = ($template.Matches[0].Groups[1].Value -split '\\n' | ForEach-Object { "// $_" }) -join "`r`n"
            foreach ($probe in $probes) {
                $path = (Resolve-Path -LiteralPath $probe).Path
                $content = [IO.File]::ReadAllText($path)
                $leadingComments = [regex]::Match($content, '\A\s*(?:(?://[^\r\n]*\r?\n|/\*[\s\S]*?\*/)\s*)*').Value
                if ($leadingComments -notmatch '\b(copyright|license|licensed|spdx-license-identifier)\b') {
                    [IO.File]::WriteAllText($path, "$header`r`n`r`n$content")
                }
            }
            dotnet tool run jb -- cleanupcode @probes --settings=eng/Format.DotSettings --no-updates --no-buildin-settings
            if ($LASTEXITCODE -ne 0) { throw 'Charts package probe formatting failed.' }
        }
    }

    if (-not $limited -or $xaml.Count -gt 0) {
        if ($limited) {
            foreach ($file in $xaml) {
                dotnet tool run xstyler -- --config Settings.XamlStyler --file (Join-Path $repositoryRoot $file)
                if ($LASTEXITCODE -ne 0) { throw "XAML Styler failed for '$file'." }
            }
        }
        else {
            $xamlDirectories = Get-ChildItem -LiteralPath 'components', 'gallery' -Filter '*.xaml' -File -Recurse |
                Where-Object { $_.FullName.Substring($repositoryRoot.Length) -notmatch '[\\/](bin|obj|artifacts|TestResults)[\\/]' } |
                Select-Object -ExpandProperty DirectoryName -Unique
            foreach ($directory in $xamlDirectories) {
                dotnet tool run xstyler -- --config Settings.XamlStyler --directory $directory
                if ($LASTEXITCODE -ne 0) { throw "XAML Styler failed in '$directory'." }
            }
        }
    }
}
finally {
    Pop-Location
}
