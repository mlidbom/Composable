Param(
	[string]$Configuration="Debug",
	[string]$OutputDirectory="..\NuGetFeed",
	[string]$PreVersion = "",
	[string]$BuildNumber=-1
)

$ErrorActionPreference="Stop"

trap {
	Write-Output $_
	exit 1
}

$scriptRoot = Split-Path (Resolve-Path $myInvocation.MyCommand.Path) 
$OutputDirectory = Resolve-Path "$scriptRoot\$OutputDirectory"

Set-Alias Build-Pkg-Internal $scriptRoot\tools\NuGet\NuGet.exe

if($BuildNumber -eq -1 -and $env:BUILD_NUMBER -ne $null) {
 	$BuildNumber = $env:BUILD_NUMBER	
}

$Version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\System\System\Bin\$Configuration\Composable.Core.dll").ProductVersion
if($PreVersion -ne '' -and $PreVersion -ne $null)
{
	$Version = $Version -replace '.0$', "$PreVersion$BuildNumber"
}else
{
	$Version = $Version -replace '.0$', ".$BuildNumber"
}
Write-Output "##teamcity[buildNumber '$Version']"


function Build-Pkg ($ProjectFile)
{
	Write-Host -ForegroundColor Cyan "Packaging $ProjectFile into $OutputDirectory"
	Build-Pkg-Internal pack $ProjectFile -OutputDirectory $OutputDirectory -Prop ComposableVersion=$Version
	if($LASTEXITCODE -ne 0)
	{
		Write-Error "Bailing out because nuget.exe exited with code $LASTEXITCODE"
	}
	Write-Host
}

Build-Pkg "$scriptRoot\System\System\Composable.Core.csproj" $CoreVersion
Build-Pkg "$scriptRoot\CQRS\CQRS\Composable.CQRS.csproj"
Build-Pkg "$scriptRoot\CQRS\NHibernateRepositories\Composable.CQRS.NHibernate.csproj"
Build-Pkg "$scriptRoot\CQRS\Composable.CQRS.ServiceBus.NServiceBus\Composable.CQRS.ServiceBus.NServiceBus.csproj" $NServiceBusVersion
Build-Pkg "$scriptRoot\Composable.DomainEvents\Composable.DomainEvents.csproj"
Build-Pkg "$scriptRoot\AutoMapper\Composable.AutoMapper\Composable.AutoMapper.csproj"
Build-Pkg "$scriptRoot\CQRS\CQRS.Windsor\Composable.CQRS.Windsor.csproj" $WindsorVersion
Build-Pkg "$scriptRoot\CQRS\Testing\Composable.CQRS.Testing\Composable.CQRS.Testing.csproj"
Build-Pkg "$scriptRoot\CQRS\Composable.CQRS.Population.Client\Composable.CQRS.Population.Client.csproj"
Build-Pkg "$scriptRoot\CQRS\Composable.CQRS.Population.Server\Composable.CQRS.Population.Server.csproj"
Build-Pkg "$scriptRoot\NSpecNUnit\NSpecNUnit.csproj"

