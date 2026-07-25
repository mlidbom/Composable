# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Get-RetiredNuGetPackageIds {
    <#
    .SYNOPSIS
    Reads RetiredNuGetPackageIds.txt — the record of package identities Compze no longer publishes under

    .DESCRIPTION
    Each recorded line names a retired package id and what replaced it. Comment lines start with '#';
    blank lines are ignored. A line that does not carry a successor is a malformed record and fails the
    read rather than being skipped: silently dropping it would silently drop a package from unlisting.

    .OUTPUTS
    Array of objects with PackageId and ReplacedBy, in file order
    #>
    [CmdletBinding()]
    param()

    $recordPath = Join-Path $script:CompzeRoot "RetiredNuGetPackageIds.txt"
    if (-not (Test-Path $recordPath)) {
        Write-Error "Retired package record not found: $recordPath"
        return @()
    }

    $retired = @()
    $lineNumber = 0
    foreach ($line in (Get-Content $recordPath)) {
        $lineNumber++
        $trimmed = $line.Trim()
        if ($trimmed -eq '' -or $trimmed.StartsWith('#')) { continue }

        if ($trimmed -notmatch '^(?<packageId>\S+)\s*->\s*(?<replacedBy>\S.*)$') {
            Write-Error "RetiredNuGetPackageIds.txt line ${lineNumber}: expected '<package id> -> <what replaced it>' but found: $trimmed"
            return @()
        }

        $retired += [PSCustomObject]@{
            PackageId  = $Matches.packageId
            ReplacedBy = $Matches.replacedBy.Trim()
        }
    }

    return $retired
}
