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
$LocalOutputDirectory = Resolve-Path "$scriptRoot\NuGetFeed"
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

function Get-Version{
	param([string]$assemblyPath)
	(FixVersion ([System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\$assemblyPath")) $PreVersion)
}

Set-Alias paket $scriptRoot\.paket\paket.exe

paket pack `
	output $LocalOutputDirectory `
	buildconfig Debug `
	buildplatform AnyCPU `
	symbols `
	specific-version Composable.Contracts (Get-Version "Composable.Contracts\bin\$Configuration\Composable.Contracts.dll") `
	specific-version Composable.Core (Get-Version "Composable.System\Bin\$Configuration\Composable.Core.dll") `
	specific-version Composable.CQRS (Get-Version "Composable.CQRS\Bin\$Configuration\Composable.CQRS.dll") `
	specific-version Composable.CQRS.NHibernate (Get-Version "Composable.CQRS.NHibernate\Bin\$Configuration\Composable.CQRS.NHibernate.dll") `
	specific-version Composable.Persistence.ORM.NHibernate (Get-Version "Composable.Persistence.ORM.NHibernate\Bin\$Configuration\Composable.Persistence.ORM.NHibernate.dll") `
	specific-version Composable.CQRS.ServiceBus.NServiceBus (Get-Version "Composable.CQRS.ServiceBus.NServiceBus\Bin\$Configuration\Composable.CQRS.ServiceBus.NServiceBus.dll") `
	specific-version Composable.DomainEvents (Get-Version "Composable.DomainEvents\Bin\$Configuration\Composable.DomainEvents.dll") `
	specific-version Composable.AutoMapper (Get-Version "Composable.AutoMapper\Bin\$Configuration\Composable.AutoMapper.dll") `
	specific-version Composable.CQRS.Windsor (Get-Version "Composable.CQRS.Windsor\Bin\$Configuration\Composable.CQRS.Windsor.dll") `
	specific-version Composable.Windsor (Get-Version "Composable.Windsor\Bin\$Configuration\Composable.Windsor.dll") `
	specific-version Composable.CQRS.Testing (Get-Version "Composable.CQRS.Testing\Bin\$Configuration\Composable.CQRS.Testing.dll") `
	specific-version NSpec.NUnit (Get-Version "NSpec.NUnit\Bin\$Configuration\NSpec.NUnit.dll")

Copy-Item -Path "$LocalOutputDirectory\*.nupkg" -Force $OutputDirectory
Remove-Item -Path "$LocalOutputDirectory\*symbols.nupkg"
