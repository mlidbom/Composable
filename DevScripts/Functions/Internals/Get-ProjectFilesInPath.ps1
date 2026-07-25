# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Get-ProjectFilesInPath {
    <#
    .SYNOPSIS
    Gets all .csproj files in a directory, excluding nCrunchTemp
    
    .DESCRIPTION
    Recursively finds all .csproj files in the specified path,
    automatically filtering out nCrunchTemp directories
    
    .PARAMETER Path
    Path to search for .csproj files
    
    .PARAMETER Recurse
    Whether to search recursively (default: $true)
    
    .OUTPUTS
    Array of FileInfo objects for all .csproj files, excluding nCrunchTemp
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        
        [Parameter(Mandatory = $false)]
        [bool]$Recurse = $true
    )
    
    if (-not (Test-Path $Path)) {
        Write-Error "Path not found: $Path"
        return @()
    }
    
    return Get-CsprojFiles -Path $Path -Recurse $Recurse
}
