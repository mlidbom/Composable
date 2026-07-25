# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Get-ProjectReferences {
    <#
    .SYNOPSIS
    Gets all ProjectReference elements from a .csproj file
    
    .DESCRIPTION
    Parses a .csproj file and extracts all ProjectReference Include paths
    
    .PARAMETER CsprojPath
    Path to the .csproj file
    
    .OUTPUTS
    Array of ProjectReference Include attribute values
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$CsprojPath
    )
    
    if (-not (Test-Path $CsprojPath)) {
        Write-Error "Project file not found: $CsprojPath"
        return @()
    }
    
    [xml]$xml = Get-Content $CsprojPath
    $projectReferences = $xml.SelectNodes("//ProjectReference[@Include]")
    
    if ($projectReferences) {
        return $projectReferences | ForEach-Object { $_.GetAttribute("Include") }
    }
    
    return @()
}
