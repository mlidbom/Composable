Set-Alias Build-Pkg .\packages\NuGet.CommandLine.1.5.20905.5\tools\NuGet.exe

Build-Pkg pack ".\System\System\Composable.Core.csproj" -OutputDirectory "..\NuGetFeed"
Build-Pkg pack ".\CQRS\CQRS\Composable.CQRS.csproj" -OutputDirectory "..\NuGetFeed"
Build-Pkg pack ".\CQRS\NHibernateRepositories\Composable.CQRS.NHibernate.csproj" -OutputDirectory "..\NuGetFeed"
Build-Pkg pack ".\CQRS\Composable.CQRS.ServiceBus.NServiceBus\Composable.CQRS.ServiceBus.NServiceBus.csproj" -OutputDirectory "..\NuGetFeed"
Build-Pkg pack ".\CQRS\Composable.CQRS.ServiceBus.NServiceBus.ObjectBuilder.CastleWindsor\Composable.CQRS.ServiceBus.NServiceBus.ObjectBuilder.CastleWindsor.csproj" -OutputDirectory "..\NuGetFeed"

