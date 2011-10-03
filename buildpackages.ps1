Param(
	[string]$Configuration="Release"
)

Set-Alias Build-Pkg .\packages\NuGet.CommandLine.1.5.20905.5\tools\NuGet.exe

Build-Pkg pack ".\System\System\Composable.Core.csproj" -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration
Build-Pkg pack ".\CQRS\CQRS\Composable.CQRS.csproj" -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration
Build-Pkg pack ".\CQRS\NHibernateRepositories\Composable.CQRS.NHibernate.csproj" -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration
Build-Pkg pack ".\CQRS\Composable.CQRS.ServiceBus.NServiceBus\Composable.CQRS.ServiceBus.NServiceBus.csproj" -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration
Build-Pkg pack ".\Composable.DomainEvents\Composable.DomainEvents.csproj" -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration
Build-Pkg pack ".\AutoMapper\Composable.AutoMapper\Composable.AutoMapper.csproj" -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration
Build-Pkg pack ".\CQRS\CQRS.Windsor\Composable.CQRS.Windsor.csproj" -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration
Build-Pkg pack ".\CQRS\Testing\Composable.CQRS.Testing\Composable.CQRS.Testing.csproj" -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration
Build-Pkg pack ".\CQRS\Composable.CQRS.Population.Client\Composable.CQRS.Population.Client.csproj" -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration
Build-Pkg pack ".\CQRS\Composable.CQRS.Population.Server\Composable.CQRS.Population.Server.csproj" -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration

