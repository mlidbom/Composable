# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Find-ListedNuGetPackageIds {
    <#
    .SYNOPSIS
    Searches nuget.org and returns the ids of every listed package matching a search term

    .DESCRIPTION
    The search service indexes only listed packages, which is exactly what makes it the right way to
    discover retirement candidates: an id that still turns up here is still advertised to the world.
    A package whose every version is already unlisted does not come back at all.

    The search term matches package ids and metadata, so results are not confined to our own packages.
    Callers must decide what is theirs — see RetiredNuGetPackageIds.txt on why nuget.org cannot tell
    a retired Compze package from one published out of its own repository.

    .PARAMETER SearchTerm
    The nuget.org search query, for example "Compze"

    .OUTPUTS
    Array of package ids
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$SearchTerm
    )

    $encodedSearchTerm = [Uri]::EscapeDataString($SearchTerm)
    $searchUrl = "https://azuresearch-usnc.nuget.org/query?q=$encodedSearchTerm&take=1000&prerelease=true&semVerLevel=2.0.0"

    $results = Invoke-RestMethod -Uri $searchUrl -TimeoutSec 60
    if ($results.totalHits -gt $results.data.Count) {
        Write-Error "nuget.org reports $($results.totalHits) matches for '$SearchTerm' but returned only $($results.data.Count); the search would need paging to be complete"
        return @()
    }

    return @($results.data | ForEach-Object { $_.id })
}
