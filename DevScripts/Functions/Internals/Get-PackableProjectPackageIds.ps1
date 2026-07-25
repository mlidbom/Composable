# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Get-PackableProjectPackageIds {
    <#
    .SYNOPSIS
    Gets the NuGet package id of every project in the solution that publishes a package

    .DESCRIPTION
    The solution is the authoritative list of projects — a stray .csproj left on disk outside it
    publishes nothing, and must not be mistaken for a live package id.

    A project publishes a package when it carries a <Version>, is not marked IsPackable=false, and its
    version is not the 0.0.0-not-published-to-nuget sentinel that marks a project packable for local
    consumption only. That is the same rule Publish-NuGetWithReleases.ps1 applies to the packed output.

    .OUTPUTS
    Array of objects with PackageId, Version and ProjectPath
    #>
    [CmdletBinding()]
    param()

    [xml]$solution = Get-Content $script:CompzeSolutionPath
    $solutionRoot = Split-Path -Parent $script:CompzeSolutionPath

    $packages = @()
    foreach ($projectPathAttribute in $solution.SelectNodes('//Project/@Path')) {
        $projectPath = Join-Path $solutionRoot $projectPathAttribute.Value
        if (-not (Test-Path $projectPath)) {
            Write-Error "Solution references a project that does not exist: $($projectPathAttribute.Value)"
            return @()
        }

        [xml]$project = Get-Content $projectPath

        $version = $project.SelectSingleNode('//Version')
        if (-not $version) { continue }
        if ($version.InnerText.Trim() -eq '0.0.0-not-published-to-nuget') { continue }

        $isPackable = $project.SelectSingleNode('//IsPackable')
        if ($isPackable -and $isPackable.InnerText.Trim() -eq 'false') { continue }

        $packageId = $project.SelectSingleNode('//PackageId')
        $packages += [PSCustomObject]@{
            PackageId   = if ($packageId) { $packageId.InnerText.Trim() } else { [IO.Path]::GetFileNameWithoutExtension($projectPath) }
            Version     = $version.InnerText.Trim()
            ProjectPath = $projectPathAttribute.Value
        }
    }

    return $packages
}
