# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Get-NuGetPackageVersionListings {
    <#
    .SYNOPSIS
    Gets, for each package id, which of its versions on nuget.org are listed and which are unlisted

    .DESCRIPTION
    "Listed" is nuget.org's term for a version being visible in search and in version resolution.
    Unlisting hides a version without deleting it, so an unlisted version still exists and still
    restores by exact version. This is the only query that distinguishes the two states: the search
    service omits unlisted versions entirely, so it can never tell you a package is fully unlisted
    rather than absent.

    Reads the registration API, which carries an explicit listed flag per version. That endpoint serves
    gzipped content; PowerShell decompresses it transparently. Large packages page their versions across
    several documents, so a page without inline items is fetched by its own url.

    .PARAMETER PackageId
    The package ids to query. Queried in parallel.

    .OUTPUTS
    Array of objects with PackageId, ExistsOnNuGetOrg, ListedVersions and UnlistedVersions
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$PackageId
    )

    return $PackageId | ForEach-Object -Parallel {
        $ProgressPreference = 'SilentlyContinue'
        $packageId = $_
        $url = "https://api.nuget.org/v3/registration5-gz-semver2/$($packageId.ToLowerInvariant())/index.json"

        $response = Invoke-WebRequest -Uri $url -SkipHttpErrorCheck -TimeoutSec 30
        if ($response.StatusCode -eq 404) {
            return [PSCustomObject]@{
                PackageId        = $packageId
                ExistsOnNuGetOrg = $false
                ListedVersions   = @()
                UnlistedVersions = @()
            }
        }
        if ($response.StatusCode -ne 200) {
            throw "nuget.org returned $($response.StatusCode) for $packageId"
        }

        $listed = @()
        $unlisted = @()
        foreach ($page in ($response.Content | ConvertFrom-Json).items) {
            $versionEntries = $page.items
            if (-not $versionEntries) {
                $versionEntries = (Invoke-RestMethod -Uri $page.'@id' -TimeoutSec 30).items
            }
            foreach ($entry in $versionEntries) {
                if ($entry.catalogEntry.listed) { $listed += $entry.catalogEntry.version }
                else { $unlisted += $entry.catalogEntry.version }
            }
        }

        [PSCustomObject]@{
            PackageId        = $packageId
            ExistsOnNuGetOrg = $true
            ListedVersions   = $listed
            UnlistedVersions = $unlisted
        }
    } -ThrottleLimit 20
}
