# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Clear-NuGetCache {
    <#
    .SYNOPSIS
    Clears the NuGet cache for Compze packages

    .DESCRIPTION
    Removes cached Compze packages from the NuGet global-packages folder so that
    subset solutions pick up freshly packed versions from the local nupkgs/ feed.

    Leaves the packages this repository does not publish — see Get-PackageIdsPublishedFromOtherRepositories.

    .EXAMPLE
    C-Clear-NuGetCache
    Clears all cached Compze library packages
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param()

    $globalPackages = & dotnet nuget locals global-packages --list 2>$null
    if ($globalPackages -match 'global-packages:\s*(.+)') {
        $pkgDir = $Matches[1].Trim()
    } else {
        Write-Error "Could not determine NuGet global-packages folder"
        return
    }

    # Packages this repository never packs come from nuget.org, so clearing them would only force a re-download
    $keepPatterns = @(Get-PackageIdsPublishedFromOtherRepositories | ForEach-Object { $_.ToLowerInvariant() })

    $cleared = 0
    Get-ChildItem $pkgDir -Directory -Filter "compze.*" | ForEach-Object {
        $name = $_.Name.ToLowerInvariant()
        $keep = $false
        foreach ($pattern in $keepPatterns) {
            if ($name -eq $pattern) { $keep = $true; break }
        }
        if (-not $keep) {
            Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
            $cleared++
        }
    }

    Write-Host "$cleared Compze packages cleared from NuGet cache"
}
