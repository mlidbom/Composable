# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Get-PackageIdsPublishedFromOtherRepositories {
    <#
    .SYNOPSIS
    Gets the Compze package ids that are published from their own repository rather than from this one

    .DESCRIPTION
    These ids have no project in this solution and never will, yet they are ours and very much alive.
    From nuget.org's side they are indistinguishable from an id we retired, and from the NuGet cache's
    side they are indistinguishable from a package this repository just packed — so every command that
    reasons about "our packages" from the outside in has to know about them.

    .OUTPUTS
    Array of package ids
    #>
    [CmdletBinding()]
    param()

    return @(
        'Compze.Build.FlexRef'  # build tool, https://github.com/mlidbom/Compze.Build.FlexRef
    )
}
