Param(
	[string]$Configuration="Debug",
	[string]$OutputDirectory=".\NuGetFeed",
	[string]$PreVersion=""
)

$ErrorActionPreference="Stop"

trap {
	Write-Output $_
	exit 1
}

$scriptRoot = Split-Path (Resolve-Path $myInvocation.MyCommand.Path) 
$OutputDirectory = Resolve-Path "$scriptRoot\$OutputDirectory"

function FixVersion
{
	Param($Version, [string]$PreVersion="")
	$Version = $Version.ProductVersion

	$Major = $Version.Split(".")[0]
	$Minor = $Version.Split(".")[1]
	$Patch = $Version.Split(".")[2]
	$Build = $Version.Split(".")[3]

	if($PreVersion -ne ""){
		"$Major.$Minor.$Patch-$PreVersion"
	}else{
	    "$Major.$Minor.$Patch"
	}	
}

Set-Alias Build-Pkg-Internal $scriptRoot\tools\NuGet\NuGet.exe

$CoreVersion = (FixVersion ([System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\Composable.System\Bin\$Configuration\Composable.Core.dll")) $PreVersion)
$CQRSVersion = (FixVersion ([System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\Composable.CQRS\Bin\$Configuration\Composable.CQRS.dll")) $PreVersion)
$CQRSTestingVersion = (FixVersion ([System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\Composable.CQRS.Testing\Bin\$Configuration\Composable.CQRS.Testing.dll")) $PreVersion)
$NSpecNUnitVersion = (FixVersion ([System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\NSpec.NUnit\Bin\$Configuration\NSpec.NUnit.dll")) $PreVersion)

function Build-Pkg ($ProjectFile, $Version)
{
	Write-Host -ForegroundColor Cyan "Packaging $ProjectFile into $OutputDirectory Version: $Version"
	Build-Pkg-Internal pack $ProjectFile -OutputDirectory $OutputDirectory `
	    -Version $Version `
		-Prop CoreVersion=$CoreVersion `
		-Prop CQRSVersion=$CQRSVersion `
		-Prop DomainEventsVersion=$DomainEventsVersion `
		-Prop CQRSTestingVersion=$CQRSTestingVersion `
		-Prop NSpecNUnitVersion=$NSpecNUnitVersion `
		 -Symbols
	
	if($LASTEXITCODE -ne 0)
	{
		Write-Error "Bailing out because nuget.exe exited with code $LASTEXITCODE"
	}
	Write-Host
}

Build-Pkg "$scriptRoot\Composable.System\Composable.Core.csproj" $CoreVersion
Build-Pkg "$scriptRoot\Composable.CQRS\Composable.CQRS.csproj" $CQRSVersion
Build-Pkg "$scriptRoot\Composable.CQRS.Testing\Composable.CQRS.Testing.csproj" $CQRSTestingVersion
Build-Pkg "$scriptRoot\NSpec.NUnit\NSpec.NUnit.csproj" $NSpecNUnitVersion
