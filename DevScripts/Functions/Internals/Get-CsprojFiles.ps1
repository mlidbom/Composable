# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Get-CsprojFiles {
    <#
    .SYNOPSIS
    Gets .csproj files, automatically excluding nCrunchTemp directories
    
    .DESCRIPTION
    Finds .csproj files in the specified path, always filtering out nCrunchTemp directories.
    This is the central function for finding .csproj files - all other functions should use this.
    
    .PARAMETER Path
    Path to search for .csproj files
    
    .PARAMETER Filter
    Optional filename filter (e.g., "MyProject.csproj")
    
    .PARAMETER Recurse
    Whether to search recursively (default: $true)
    
    .OUTPUTS
    Array of FileInfo objects for .csproj files, excluding nCrunchTemp
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        
        [Parameter(Mandatory = $false)]
        [string]$Filter = "*.csproj",
        
        [Parameter(Mandatory = $false)]
        [bool]$Recurse = $true
    )
    
    if (-not (Test-Path $Path)) {
        Write-Error "Path not found: $Path"
        return @()
    }
    
    $files = if ($Recurse) {
        Get-ChildItem -Path $Path -Filter $Filter -Recurse
    } else {
        Get-ChildItem -Path $Path -Filter $Filter
    }
    
    # SINGLE PLACE where nCrunchTemp filtering happens
    return $files | Where-Object { $_.FullName -notmatch '\\nCrunchTemp' }
}
