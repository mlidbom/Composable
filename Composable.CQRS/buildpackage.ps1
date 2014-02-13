
Param(
    [string]$ProjectFile,
    [string]$OutputDirectory)


$scriptRoot = Split-Path (Resolve-Path $myInvocation.MyCommand.Path) 

Set-Alias Build-Pkg-Internal $scriptRoot\..\.nuget\NuGet.exe
Build-Pkg-Internal pack $ProjectFile -OutputDirectory $OutputDirectory