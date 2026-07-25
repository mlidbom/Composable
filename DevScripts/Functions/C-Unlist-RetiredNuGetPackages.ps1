# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Unlist-RetiredNuGetPackages {
    <#
    .SYNOPSIS
    Unlists from nuget.org every still-listed version of every package id recorded as retired

    .DESCRIPTION
    Unlisting hides a package version from nuget.org search and from version resolution. It is not a
    delete: nuget.org does not support deleting, and anyone already depending on an exact version can
    still restore it. What it stops is new adoption of an identity we have abandoned.

    Prints the plan and stops. Pass -Execute to carry it out.

    Refuses to run while any recorded id belongs to a project in the solution, because that means the
    record is stale and running it would hide a live package.

    .PARAMETER Execute
    Carry out the plan instead of only printing it

    .PARAMETER ApiKey
    A nuget.org API key with the Unlist scope, defaulting to the NUGET_UNLIST_API_KEY environment
    variable. This is deliberately not the key CI publishes with: nuget.org scopes Push and Unlist
    separately, so a push key cannot unlist, and a key that can do both would let a broken publish run
    hide packages.

    .EXAMPLE
    C-Unlist-RetiredNuGetPackages
    Prints what would be unlisted and changes nothing.

    .EXAMPLE
    C-Unlist-RetiredNuGetPackages -Execute
    Unlists every still-listed version of every recorded retired package id.
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [switch]$Execute,
        [string]$ApiKey = $env:NUGET_UNLIST_API_KEY
    )

    $retired = @(Get-RetiredNuGetPackageIds)
    if ($retired.Count -eq 0) { return }

    $solutionPackageIds = @(Get-PackableProjectPackageIds | ForEach-Object { $_.PackageId })
    $reclaimed = @($retired | Where-Object { $solutionPackageIds -contains $_.PackageId } | ForEach-Object { $_.PackageId })
    if ($reclaimed.Count -gt 0) {
        Write-Error "RetiredNuGetPackageIds.txt records ids that projects in the solution publish: $($reclaimed -join ', '). Remove them from the record before unlisting anything."
        return
    }

    $listings = @(Get-NuGetPackageVersionListings -PackageId @($retired | ForEach-Object { $_.PackageId }))
    $toUnlist = @($listings | Where-Object { @($_.ListedVersions).Count -gt 0 } | Sort-Object PackageId)
    if ($toUnlist.Count -eq 0) {
        Write-Host "Every retired package id is already unlisted."
        return
    }

    $versionCount = ($toUnlist | ForEach-Object { @($_.ListedVersions).Count } | Measure-Object -Sum).Sum
    Write-Host "Unlist $versionCount version(s) across $($toUnlist.Count) retired package id(s) from nuget.org:"
    foreach ($listing in $toUnlist) {
        Write-Host "  $($listing.PackageId)  ->  $(($retired | Where-Object { $_.PackageId -eq $listing.PackageId }).ReplacedBy)"
        foreach ($version in @($listing.ListedVersions)) { Write-Host "      $version" }
    }
    Write-Host ""

    if (-not $Execute) {
        Write-Host "Nothing was changed. Re-run as 'C-Unlist-RetiredNuGetPackages -Execute' to unlist the versions above."
        Write-Host "nuget.org offers no API to re-list: undoing this means one checkbox per version on the website."
        return
    }

    if (-not $ApiKey) {
        Write-Error "No API key. Set NUGET_UNLIST_API_KEY, or pass -ApiKey, with a nuget.org key scoped to Unlist for the glob pattern Compze.*"
        return
    }

    $failures = @()
    foreach ($listing in $toUnlist) {
        foreach ($version in @($listing.ListedVersions)) {
            dotnet nuget delete $listing.PackageId $version `
                --source "https://api.nuget.org/v3/index.json" `
                --api-key $ApiKey `
                --non-interactive
            if ($LASTEXITCODE -ne 0) { $failures += "$($listing.PackageId) $version" }
        }
    }

    if ($failures.Count -gt 0) {
        Write-Error "Failed to unlist $($failures.Count) of $versionCount version(s): $($failures -join ', ')"
        return
    }

    Write-Host "Unlisted $versionCount version(s). nuget.org's search index takes a few minutes to catch up, so run C-Get-RetiredNuGetPackageStatus after that to confirm."
}
