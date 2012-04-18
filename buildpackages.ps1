Param(
	[string]$Configuration="Release",
	[string]$OutputDirectory="..\NuGetFeed"
)

$ErrorActionPreference="Stop"

trap {
	Write-Output $_
	exit 1
}

$scriptRoot = Split-Path (Resolve-Path $myInvocation.MyCommand.Path) 
$OutputDirectory = Resolve-Path "$scriptRoot\$OutputDirectory"

Set-Alias Build-Pkg-Internal $scriptRoot\packages\NuGet.CommandLine.1.7.0\tools\NuGet.exe

function Build-Pkg ($ProjectFile)
{
	Write-Host -ForegroundColor Cyan "Packaging $ProjectFile into $OutputDirectory"
	Build-Pkg-Internal pack $ProjectFile -OutputDirectory $OutputDirectory -Prop Configuration=$Configuration -Prop CoreVersion=$CoreVersion -Prop CqrsVersion=$CqrsVersion -Prop WindsorVersion=$WindsorVersion -Prop DomainEventsVersion=$DomainEventsVersion -Prop NServiceBusVersion=$NServiceBusVersion -Prop AutoMapperVersion=$AutoMapperVersion
	if($LASTEXITCODE -ne 0)
	{
		Write-Error "Bailing out because nuget.exe exited with code $LASTEXITCODE"
	}
	Write-Host
}

function GetAssemblyVersion($assembly)
{   
   $ErrorActionPreference="Stop"
   $assembly = "$scriptRoot\$assembly"	
   $Myasm = [System.Reflection.Assembly]::Loadfile($assembly)   
   $Aname = $Myasm.GetName()
   $Aver =  $Aname.version
   return $Aver
}

$CoreVersion = GetAssemblyVersion("System\System\Bin\$Configuration\Composable.Core.dll")
$WindsorVersion = GetAssemblyVersion("CQRS\CQRS.Windsor\Bin\$Configuration\Composable.CQRS.Windsor.dll")
$NServiceBusVersion = GetAssemblyVersion("CQRS\Composable.CQRS.ServiceBus.NServiceBus\Bin\$Configuration\Composable.CQRS.ServiceBus.NServiceBus.dll")
$CqrsVersion = GetAssemblyVersion("CQRS\CQRS\Bin\$Configuration\Composable.CQRS.dll")
$DomainEventsVersion = GetAssemblyVersion("Composable.DomainEvents\Bin\$Configuration\Composable.DomainEvents.dll")
$AutoMapperVersion = GetAssemblyVersion("AutoMapper\Composable.AutoMapper\Bin\$Configuration\Composable.AutoMapper.dll")

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

