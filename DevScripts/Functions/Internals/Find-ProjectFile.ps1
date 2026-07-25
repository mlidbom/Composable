# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Find-ProjectFile {
    <#
    .SYNOPSIS
    Finds a specific .csproj file by project name
    
    .DESCRIPTION
    Recursively searches for a .csproj file with the given project name,
    excluding nCrunchTemp directories
    
    .PARAMETER SolutionPath
    Path to the solution file
    
    .PARAMETER ProjectName
    Name of the project to find (without .csproj extension)
    
    .OUTPUTS
    FileInfo object for the project file, or $null if not found
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$SolutionPath,
        
        [Parameter(Mandatory = $true)]
        [string]$ProjectName
    )
    
    if (-not (Test-Path $SolutionPath)) {
        Write-Error "Solution file not found: $SolutionPath"
        return $null
    }
    
    # The solution lives at the repository root, so its directory is the repo root.
    $repoRoot = Split-Path -Parent $SolutionPath
    $projectFileName = "$ProjectName.csproj"
    
    return Get-CsprojFiles -Path $repoRoot -Filter $projectFileName | 
        Select-Object -First 1
}
