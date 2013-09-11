Param(
	[string]$Configuration="Debug",
	[string]$OutputDirectory="..\NuGetFeed",
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

$CoreVersion = (FixVersion ([System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\System\System\Bin\$Configuration\Composable.Core.dll")) $PreVersion)
$CQRSVersion = (FixVersion ([System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\CQRS\CQRS\Bin\$Configuration\Composable.CQRS.dll")) $PreVersion)
Write-Host -ForegroundColor Cyan $CQRSVersion
$CQRSNHibernateVersion = (FixVersion ([System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\CQRS\NHibernateRepositories\Bin\$Configuration\Composable.CQRS.NHibernate.dll")) $PreVersion)
$CQRSServiceBusNServicebusVersion = (FixVersion ([System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\CQRS\Composable.CQRS.ServiceBus.NServiceBus\Bin\$Configuration\Composable.CQRS.ServiceBus.NServiceBus.dll")) $PreVersion)
$DomainEventsVersion = (FixVersion ([System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\Composable.DomainEvents\Bin\$Configuration\Composable.DomainEvents.dll")) $PreVersion)
$AutomapperVersion = (FixVersion ([System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\AutoMapper\Composable.AutoMapper\Bin\$Configuration\Composable.AutoMapper.dll")) $PreVersion)
$CQRSWindsorVersion = (FixVersion ([System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\CQRS\CQRS.Windsor\Bin\$Configuration\Composable.CQRS.Windsor.dll")) $PreVersion)
$CQRSTestingVersion = (FixVersion ([System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\CQRS\Testing\Composable.CQRS.Testing\Bin\$Configuration\Composable.CQRS.Testing.dll")) $PreVersion)
$NSpecNUnitVersion = (FixVersion ([System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\NSpec.NUnit\Bin\$Configuration\NSpec.NUnit.dll")) $PreVersion)

function Build-Pkg ($ProjectFile, $Version)
{
	Write-Host -ForegroundColor Cyan "Packaging $ProjectFile into $OutputDirectory Version: $Version"
	Build-Pkg-Internal pack $ProjectFile -OutputDirectory $OutputDirectory `
	    -Version $Version `
		-Prop CoreVersion=$CoreVersion `
		-Prop CQRSVersion=$CQRSVersion `
		-Prop CQRSNHibernateVersion=$CQRSNHibernateVersion `
		-Prop CQRSServiceBusNServicebusVersion=$CQRSServiceBusNServicebusVersion `
		-Prop DomainEventsVersion=$DomainEventsVersion `
		-Prop AutomapperVersion=$AutomapperVersion `
		-Prop CQRSWindsorVersion=$CQRSWindsorVersion `
		-Prop CQRSTestingVersion=$CQRSTestingVersion `
		-Prop CQRSPopulationClientVersion=$CQRSPopulationClientVersion `
		-Prop CJRSPopulationServerVersion=$CJRSPopulationServerVersion `
		-Prop NSpecNUnitVersion=$NSpecNUnitVersion `
		 -Symbols
	
	if($LASTEXITCODE -ne 0)
	{
		Write-Error "Bailing out because nuget.exe exited with code $LASTEXITCODE"
	}
	Write-Host
}

Build-Pkg "$scriptRoot\System\System\Composable.Core.csproj" $CoreVersion
Build-Pkg "$scriptRoot\CQRS\CQRS\Composable.CQRS.csproj" $CQRSVersion
Build-Pkg "$scriptRoot\CQRS\NHibernateRepositories\Composable.CQRS.NHibernate.csproj" $CQRSNHibernateVersion
Build-Pkg "$scriptRoot\CQRS\Composable.CQRS.ServiceBus.NServiceBus\Composable.CQRS.ServiceBus.NServiceBus.csproj" $CQRSServiceBusNServicebusVersion
Build-Pkg "$scriptRoot\Composable.DomainEvents\Composable.DomainEvents.csproj" $DomainEventsVersion
Build-Pkg "$scriptRoot\AutoMapper\Composable.AutoMapper\Composable.AutoMapper.csproj" $AutomapperVersion
Build-Pkg "$scriptRoot\CQRS\CQRS.Windsor\Composable.CQRS.Windsor.csproj" $CQRSWindsorVersion
Build-Pkg "$scriptRoot\CQRS\Testing\Composable.CQRS.Testing\Composable.CQRS.Testing.csproj" $CQRSTestingVersion
Build-Pkg "$scriptRoot\NSpec.NUnit\NSpec.NUnit.csproj" $NSpecNUnitVersion
