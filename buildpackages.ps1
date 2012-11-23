Param(
	[string]$Configuration="Debug",
	[string]$OutputDirectory="..\NuGetFeed"
)

$ErrorActionPreference="Stop"

trap {
	Write-Output $_
	exit 1
}

$scriptRoot = Split-Path (Resolve-Path $myInvocation.MyCommand.Path) 
$OutputDirectory = Resolve-Path "$scriptRoot\$OutputDirectory"

Set-Alias Build-Pkg-Internal $scriptRoot\tools\NuGet\NuGet.exe

$CoreVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\System\System\Bin\$Configuration\Composable.Core.dll").ProductVersion
$CQRSVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\CQRS\CQRS\Bin\$Configuration\Composable.CQRS.dll").ProductVersion
$CQRSNHibernateVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\CQRS\NHibernateRepositories\Bin\$Configuration\Composable.CQRS.NHibernate.dll").ProductVersion
$CQRSServiceBusNServicebusVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\CQRS\Composable.CQRS.ServiceBus.NServiceBus\Bin\$Configuration\Composable.CQRS.ServiceBus.NServiceBus.dll").ProductVersion
$DomainEventsVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\Composable.DomainEvents\Bin\$Configuration\Composable.DomainEvents.dll").ProductVersion
$AutomapperVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\AutoMapper\Composable.AutoMapper\Bin\$Configuration\Composable.AutoMapper.dll").ProductVersion
$CQRSWindsorVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\CQRS\CQRS.Windsor\Bin\$Configuration\Composable.CQRS.Windsor.dll").ProductVersion
$CQRSTestingVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\CQRS\Testing\Composable.CQRS.Testing\Bin\$Configuration\Composable.CQRS.Testing.dll").ProductVersion
$CQRSPopulationClientVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\CQRS\Composable.CQRS.Population.Client\Bin\$Configuration\Composable.CQRS.Population.Client.dll").ProductVersion
$CJRSPopulationServerVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\CQRS\Composable.CQRS.Population.Server\Bin\$Configuration\Composable.CQRS.Population.Server.dll").ProductVersion
$NSpecNUnitVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\NSpec.NUnit\Bin\$Configuration\NSpec.NUnit.dll").ProductVersion

function Build-Pkg ($ProjectFile)
{
	Write-Host -ForegroundColor Cyan "Packaging $ProjectFile into $OutputDirectory"
	Build-Pkg-Internal pack $ProjectFile -OutputDirectory $OutputDirectory `
		-Prop CoreVersion=$CoreVersion -Prop CQRSVersion=$CQRSVersion `
		-Prop CQRSNHibernateVersion=$CQRSNHibernateVersion `
		-Prop CQRSServiceBusNServicebusVersion=$CQRSServiceBusNServicebusVersion `
		-Prop DomainEventsVersion=$DomainEventsVersion `
		-Prop AutomapperVersion=$AutomapperVersion `
		-Prop CQRSWindsorVersion=$CQRSWindsorVersion `
		-Prop CQRSTestingVersion=$CQRSTestingVersion `
		-Prop CQRSPopulationClientVersion=$CQRSPopulationClientVersion `
		-Prop CJRSPopulationServerVersion=$CJRSPopulationServerVersion `
		-Prop NSpecNUnitVersion=$NSpecNUnitVersion `
	
	if($LASTEXITCODE -ne 0)
	{
		Write-Error "Bailing out because nuget.exe exited with code $LASTEXITCODE"
	}
	Write-Host
}

Build-Pkg "$scriptRoot\System\System\Composable.Core.csproj"
Build-Pkg "$scriptRoot\CQRS\CQRS\Composable.CQRS.csproj"
Build-Pkg "$scriptRoot\CQRS\NHibernateRepositories\Composable.CQRS.NHibernate.csproj"
Build-Pkg "$scriptRoot\CQRS\Composable.CQRS.ServiceBus.NServiceBus\Composable.CQRS.ServiceBus.NServiceBus.csproj"
Build-Pkg "$scriptRoot\Composable.DomainEvents\Composable.DomainEvents.csproj"
Build-Pkg "$scriptRoot\AutoMapper\Composable.AutoMapper\Composable.AutoMapper.csproj"
Build-Pkg "$scriptRoot\CQRS\CQRS.Windsor\Composable.CQRS.Windsor.csproj"
Build-Pkg "$scriptRoot\CQRS\Testing\Composable.CQRS.Testing\Composable.CQRS.Testing.csproj"
Build-Pkg "$scriptRoot\CQRS\Composable.CQRS.Population.Client\Composable.CQRS.Population.Client.csproj"
Build-Pkg "$scriptRoot\CQRS\Composable.CQRS.Population.Server\Composable.CQRS.Population.Server.csproj"
Build-Pkg "$scriptRoot\NSpec.NUnit\NSpec.NUnit.csproj"

