function Get-PackageUri([string]$Id, [string]$Version) {
    $idPath = $Id.ToLowerInvariant()
    $versionPath = $Version.Split('+')[0].ToLowerInvariant()
    [uri]"https://api.nuget.org/v3-flatcontainer/$idPath/$versionPath/$idPath.$versionPath.nupkg"
}

function Wait-PackageIndex([string]$Id, [string]$Version, [datetime]$Deadline) {
    $uri = Get-PackageUri $Id $Version
    $response = Invoke-WebRequest -Uri $uri -Method Head -SkipHttpErrorCheck
    while ($response.StatusCode -eq 404 -and [DateTime]::UtcNow -lt $Deadline) {
        Write-Host "Waiting for NuGet.org to index $Id $Version."
        Start-Sleep -Seconds 15
        $response = Invoke-WebRequest -Uri $uri -Method Head -SkipHttpErrorCheck
    }
    if ($response.StatusCode -ne 200) {
        throw "NuGet.org must index $Id $Version before its dependents can be published (HTTP $($response.StatusCode))."
    }
}
