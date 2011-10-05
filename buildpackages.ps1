Param(
	[string]$Configuration="Release"
)

$ErrorActionPreference="Stop"

Set-Alias Build-Pkg .\packages\NuGet.CommandLine.1.5.20905.5\tools\NuGet.exe

$scriptRoot = Split-Path (Resolve-Path $myInvocation.MyCommand.Path)

function GetAssemblyVersion($assembly)
{   
   $assembly = $scriptRoot + "\Bin\" + $Configuration + "\" + $assembly
   write-host $assembly
   $Myasm = [System.Reflection.Assembly]::Loadfile($assembly)   
   $Aname = $Myasm.GetName()
   $Aver =  $Aname.version
   return $Aver
}

$CoreVersion = GetAssemblyVersion("Composable.Core.dll")
$WindsorVersion = GetAssemblyVersion("Composable.CQRS.Windsor.dll")
$NServiceBusVersion = GetAssemblyVersion("Composable.CQRS.ServiceBus.NServiceBus.dll")
$CqrsVersion = GetAssemblyVersion("Composable.CQRS.dll")
$DomainEventsVersion = GetAssemblyVersion("Composable.DomainEvents.dll")
$AutoMapperVersion = GetAssemblyVersion("Composable.AutoMapper.dll")


Build-Pkg pack ".\System\System\Composable.Core.csproj" -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration
Build-Pkg pack ".\CQRS\CQRS\Composable.CQRS.csproj" -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration -Prop CoreVersion=$CoreVersion -Prop CqrsVersion=$CqrsVersion -Prop WindsorVersion=$WindsorVersion -Prop DomainEventsVersion=$DomainEventsVersion -Prop NServiceBusVersion=$NServiceBusVersion -Prop AutoMapperVersion=$AutoMapperVersion
Build-Pkg pack ".\CQRS\NHibernateRepositories\Composable.CQRS.NHibernate.csproj" -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration -Prop CoreVersion=$CoreVersion -Prop CqrsVersion=$CqrsVersion -Prop WindsorVersion=$WindsorVersion -Prop DomainEventsVersion=$DomainEventsVersion -Prop NServiceBusVersion=$NServiceBusVersion -Prop AutoMapperVersion=$AutoMapperVersion
Build-Pkg pack ".\CQRS\Composable.CQRS.ServiceBus.NServiceBus\Composable.CQRS.ServiceBus.NServiceBus.csproj" -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration -Prop CoreVersion=$CoreVersion -Prop CqrsVersion=$CqrsVersion -Prop WindsorVersion=$WindsorVersion -Prop DomainEventsVersion=$DomainEventsVersion -Prop NServiceBusVersion=$NServiceBusVersion -Prop AutoMapperVersion=$AutoMapperVersion
Build-Pkg pack ".\Composable.DomainEvents\Composable.DomainEvents.csproj" -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration -Prop CoreVersion=$CoreVersion -Prop CqrsVersion=$CqrsVersion -Prop WindsorVersion=$WindsorVersion -Prop DomainEventsVersion=$DomainEventsVersion -Prop NServiceBusVersion=$NServiceBusVersion -Prop AutoMapperVersion=$AutoMapperVersion
Build-Pkg pack ".\AutoMapper\Composable.AutoMapper\Composable.AutoMapper.csproj" -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration -Prop CoreVersion=$CoreVersion -Prop CqrsVersion=$CqrsVersion -Prop WindsorVersion=$WindsorVersion -Prop DomainEventsVersion=$DomainEventsVersion -Prop NServiceBusVersion=$NServiceBusVersion -Prop AutoMapperVersion=$AutoMapperVersion
Build-Pkg pack ".\CQRS\CQRS.Windsor\Composable.CQRS.Windsor.csproj" -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration -Prop CoreVersion=$CoreVersion -Prop CqrsVersion=$CqrsVersion -Prop WindsorVersion=$WindsorVersion -Prop DomainEventsVersion=$DomainEventsVersion -Prop NServiceBusVersion=$NServiceBusVersion -Prop AutoMapperVersion=$AutoMapperVersion
Build-Pkg pack ".\CQRS\Testing\Composable.CQRS.Testing\Composable.CQRS.Testing.csproj" -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration -Prop CoreVersion=$CoreVersion -Prop CqrsVersion=$CqrsVersion -Prop WindsorVersion=$WindsorVersion -Prop DomainEventsVersion=$DomainEventsVersion -Prop NServiceBusVersion=$NServiceBusVersion -Prop AutoMapperVersion=$AutoMapperVersion
Build-Pkg pack ".\CQRS\Composable.CQRS.Population.Client\Composable.CQRS.Population.Client.csproj" -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration -Prop CoreVersion=$CoreVersion -Prop CqrsVersion=$CqrsVersion -Prop WindsorVersion=$WindsorVersion -Prop DomainEventsVersion=$DomainEventsVersion -Prop NServiceBusVersion=$NServiceBusVersion -Prop AutoMapperVersion=$AutoMapperVersion
Build-Pkg pack ".\CQRS\Composable.CQRS.Population.Server\Composable.CQRS.Population.Server.csproj" -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration -Prop CoreVersion=$CoreVersion -Prop CqrsVersion=$CqrsVersion -Prop WindsorVersion=$WindsorVersion -Prop DomainEventsVersion=$DomainEventsVersion -Prop NServiceBusVersion=$NServiceBusVersion -Prop AutoMapperVersion=$AutoMapperVersion

