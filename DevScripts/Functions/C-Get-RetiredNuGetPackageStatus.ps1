# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Get-RetiredNuGetPackageStatus {
    <#
    .SYNOPSIS
    Reports how the record of retired package identities compares to nuget.org and to the solution

    .DESCRIPTION
    Reads RetiredNuGetPackageIds.txt, the projects in the solution, and every listed Compze package on
    nuget.org, and reports the three ways those three can disagree:

    RECLAIMED — an id recorded as retired that a project in the solution now publishes. This blocks
    C-Unlist-RetiredNuGetPackages, because unlisting the id would hide a live package.

    UNRECORDED — an id still advertised on nuget.org with no project left in the solution. Renaming a
    project leaves its old id behind exactly like this. Each one is a retirement candidate, not a
    verdict: confirm it is really ours and really retired before recording it.

    AWAITING UNLISTING — a recorded id that still has listed versions, which is the work
    C-Unlist-RetiredNuGetPackages exists to do.

    Reads only. Changes nothing here or on nuget.org.

    .EXAMPLE
    C-Get-RetiredNuGetPackageStatus
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param()

    $solutionPackageIds = @(Get-PackableProjectPackageIds | ForEach-Object { $_.PackageId })
    $retired = @(Get-RetiredNuGetPackageIds)
    if ($retired.Count -eq 0) { return }

    $reclaimed = @($retired | Where-Object { $solutionPackageIds -contains $_.PackageId })
    if ($reclaimed.Count -gt 0) {
        Write-Host "RECLAIMED — recorded as retired, but a project in the solution publishes it ($($reclaimed.Count)):" -ForegroundColor Red
        foreach ($entry in $reclaimed) { Write-Host "  $($entry.PackageId)" }
        Write-Host "  Remove these from RetiredNuGetPackageIds.txt. Unlisting them would hide a live package."
        Write-Host ""
    }

    $listedOnNuGetOrg = @(Find-ListedNuGetPackageIds -SearchTerm "Compze" | Where-Object { $_ -like "Compze.*" })
    $retiredIds = @($retired | ForEach-Object { $_.PackageId })
    $publishedElsewhere = @(Get-PackageIdsPublishedFromOtherRepositories)

    $unrecorded = @($listedOnNuGetOrg | Where-Object {
        $solutionPackageIds -notcontains $_ -and $retiredIds -notcontains $_ -and $publishedElsewhere -notcontains $_
    })
    if ($unrecorded.Count -gt 0) {
        Write-Host "UNRECORDED — listed on nuget.org with no project in the solution ($($unrecorded.Count)):" -ForegroundColor Yellow
        foreach ($packageId in $unrecorded) { Write-Host "  $packageId" }
        Write-Host "  Record each retired id in RetiredNuGetPackageIds.txt with what replaced it."
        Write-Host "  An id published from its own repository belongs in Get-PackageIdsPublishedFromOtherRepositories instead."
        Write-Host ""
    }

    # A reclaimed id is listed on nuget.org because it is alive. Reporting it as work to do would invite
    # exactly the unlisting that makes it a live package's last version disappear from search.
    $reclaimedIds = @($reclaimed | ForEach-Object { $_.PackageId })
    $listings = @(Get-NuGetPackageVersionListings -PackageId @($retiredIds | Where-Object { $reclaimedIds -notcontains $_ }))
    $awaitingUnlisting = @($listings | Where-Object { @($_.ListedVersions).Count -gt 0 } | Sort-Object PackageId)
    $versionCount = ($awaitingUnlisting | ForEach-Object { @($_.ListedVersions).Count } | Measure-Object -Sum).Sum

    if ($awaitingUnlisting.Count -gt 0) {
        Write-Host "AWAITING UNLISTING — retired but still listed ($($awaitingUnlisting.Count) ids, $versionCount versions):" -ForegroundColor Yellow
        $idColumnWidth = ($awaitingUnlisting | ForEach-Object { $_.PackageId.Length } | Measure-Object -Maximum).Maximum
        foreach ($listing in $awaitingUnlisting) {
            Write-Host ("  {0}  {1}" -f $listing.PackageId.PadRight($idColumnWidth), (@($listing.ListedVersions) -join ', '))
        }
        Write-Host "  Run C-Unlist-RetiredNuGetPackages to see the unlisting plan."
        Write-Host ""
    }

    $missing = @($listings | Where-Object { -not $_.ExistsOnNuGetOrg } | ForEach-Object { $_.PackageId })
    if ($missing.Count -gt 0) {
        Write-Host "Recorded as retired but never published to nuget.org, so nothing to unlist ($($missing.Count)):" -ForegroundColor DarkGray
        foreach ($packageId in $missing) { Write-Host "  $packageId" -ForegroundColor DarkGray }
        Write-Host ""
    }

    if ($reclaimed.Count -eq 0 -and $unrecorded.Count -eq 0 -and $awaitingUnlisting.Count -eq 0) {
        Write-Host "Every retired package id is unlisted, and every id listed on nuget.org has a project in the solution."
    }
}
