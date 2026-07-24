using System.Diagnostics;
using System.Globalization;
using System.Runtime.Loader;
using Compze.Abstractions.Configuration;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Microsoft;
using Compze.Hosting;
using Compze.Internals.Logging;
using Compze.Hosting.SameMachine;
using Compze.Serialization.Newtonsoft.Wiring;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.Endpoints.BestEffort;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Typermedia;
using Compze.Tessaging.Sqlite.Wiring;
using Compze.Tessaging.Transport.NamedPipes;
using Compze.TypeIdentifiers.DependencyInjection;

namespace Compze.Tests.SameMachine.EndpointHostProcess;

///<summary>A standalone process hosting one endpoint over named pipes, discovered through an<br/>
/// <see cref="InterprocessEndpointRegistry"/> — the counterpart process the multi-process specifications converse with. What<br/>
/// the endpoint speaks is the launching specification's choice, passed as the composition argument:<br/>
/// <see cref="ExactlyOnceTessagingComposition"/> handles <see cref="TommandSentToTheEndpointHostProcess"/> by sending<br/>
/// <see cref="TommandSentBackToTheSpecificationProcess"/> — proving discovery, exactly-once delivery, and reply across real process<br/>
/// boundaries with no web stack and no database server; <see cref="DatabaselessComposition"/> handles<br/>
/// <see cref="IBestEffortTeventPublishedByTheSpecificationProcess"/> by publishing<br/>
/// <see cref="BestEffortTeventPublishedByTheEndpointHostProcess"/> and answers <see cref="TueryAskedByTheSpecificationProcess"/> —<br/>
/// the guarantee-free conversation in both communication styles, with no database anywhere. Both compositions are declarations<br/>
/// building under the one shared <see cref="EndpointHostProcessEndpointIdentity"/>, each in its composition's environment.</summary>
public static class Program
{
   ///<summary>The composition argument selecting the full exactly-once Tessaging pipeline on a sqlite database — the exactly-once tommand conversation.</summary>
   public const string ExactlyOnceTessagingComposition = "ExactlyOnceTessaging";

   ///<summary>The composition argument selecting the database-less endpoint: guarantee-free distributed Tessaging plus distributed<br/>
   /// Typermedia — the best-effort tevent conversation and the tuery conversation, with nothing persisted anywhere.</summary>
   public const string DatabaselessComposition = "Databaseless";

   public static async Task<int> Main(string[] args)
   {
      MakeNCrunchInstrumentedDependenciesLoadable();

      //Everything this process logs goes to the console the launching specification captures, and Debug is where the delivery
      //machinery narrates itself - each delivery, failure, and backoff - so a cross-process failure report tells this side's
      //whole story.
      CompzeLogger.LogLevel = LogLevel.Debug;

      var registryName = args[0];
      var workDirectory = new DirectoryInfo(args[1]); //Holds the registry's backing file and, for the exactly-once composition, this process's sqlite database files.
      var specificationProcessId = int.Parse(args[2], CultureInfo.InvariantCulture);
      var composition = args[3];

      using var registry = InterprocessEndpointRegistry.OpenOrCreateSessionLocal(registryName, workDirectory);
      var host = EndpointHost.Production.Create(() => new MicrosoftContainerBuilder(new ComponentRegistrar()));
      await using(host)
      {
         switch(composition)
         {
            case ExactlyOnceTessagingComposition:
               host.RegisterEndpoint(new ExactlyOnceTessagingEndpointDeclaration(), new ExactlyOnceTessagingEnvironment(registry, workDirectory));
               break;
            case DatabaselessComposition:
               host.RegisterEndpoint(new DatabaselessEndpointDeclaration(), new DatabaselessEnvironment(registry));
               break;
            default:
               throw new ArgumentOutOfRangeException(nameof(args), composition, $"Unknown composition argument. Pass {ExactlyOnceTessagingComposition} or {DatabaselessComposition}.");
         }

         await host.StartAsync();

         //The specification process owns this process's lifetime: it exits (or is killed) when the specifications are done,
         //and we follow — so a crashed test run leaves no orphaned endpoint host processes behind.
         using var specificationProcess = Process.GetProcessById(specificationProcessId);
         await specificationProcess.WaitForExitAsync();
      }

      return 0;
   }

   ///<summary>The exactly-once composition (<see cref="ExactlyOnceTessagingComposition"/>): handles<br/>
   /// <see cref="TommandSentToTheEndpointHostProcess"/> by sending <see cref="TommandSentBackToTheSpecificationProcess"/>.</summary>
   class ExactlyOnceTessagingEndpointDeclaration : ExactlyOnceEndpointDeclaration<EndpointHostProcessEndpointIdentity>
   {
      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireMappedTypesFromAssemblyContaining<TommandSentToTheEndpointHostProcess>();

      protected override void RegisterExactlyOnceTommandHandlers(IExactlyOnceTommandHandlerRegistrar handle) => handle
         .ForTommand(async (TommandSentToTheEndpointHostProcess _, IUnitOfWorkTommandSender unitOfWorkTommandSender) =>
                        await unitOfWorkTommandSender.SendAsync(new TommandSentBackToTheSpecificationProcess()));
   }

   ///<summary>The database-less composition (<see cref="DatabaselessComposition"/>): guarantee-free distributed Tessaging plus<br/>
   /// distributed Typermedia — handles <see cref="IBestEffortTeventPublishedByTheSpecificationProcess"/> by publishing<br/>
   /// <see cref="BestEffortTeventPublishedByTheEndpointHostProcess"/>, and answers <see cref="TueryAskedByTheSpecificationProcess"/>.</summary>
   class DatabaselessEndpointDeclaration : BestEffortEndpointDeclaration<EndpointHostProcessEndpointIdentity>
   {
      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireMappedTypesFromAssemblyContaining<TommandSentToTheEndpointHostProcess>();

      ///<summary>Requiring the specification's endpoint makes the reply leg deterministic: the reply is published while handling<br/>
      /// the specification's tevent, which can happen before this process's own reconciliation has met the specification's<br/>
      /// endpoint — held for the required peer, it delivers on first contact instead of vanishing into the discovery race.</summary>
      protected override IReadOnlyList<EndpointId> RequiredPeers => [SpecificationProcessEndpointIdentity.Id];

      protected override void RegisterBestEffortTeventHandlers(IBestEffortTeventHandlerRegistrar handle) => handle
         .ForTevent((IBestEffortTeventPublishedByTheSpecificationProcess _, IUnitOfWorkTeventPublisher teventPublisher) =>
                       teventPublisher.Publish(new BestEffortTeventPublishedByTheEndpointHostProcess()));

      protected override void RegisterTueryHandlers(ITueryHandlerRegistrar handle) => handle
         .ForTuery((TueryAskedByTheSpecificationProcess _) => new AnswerToTheTueryAskedByTheSpecificationProcess(answeredBy: EndpointHostProcessEndpointIdentity.Name));
   }

   ///<summary>The environment of the exactly-once composition: named pipes, Newtonsoft serialization, participation in the<br/>
   /// shared registry, and a sqlite domain database in the work directory — connection strings resolve to sqlite files there,<br/>
   /// which is the only configuration this process reads.</summary>
   class ExactlyOnceTessagingEnvironment : IEndpointEnvironment
   {
      readonly InterprocessEndpointRegistry _registry;
      readonly DirectoryInfo _workDirectory;

      internal ExactlyOnceTessagingEnvironment(InterprocessEndpointRegistry registry, DirectoryInfo workDirectory)
      {
         _registry = registry;
         _workDirectory = workDirectory;
      }

      public void Configure(EndpointBuilder endpointBuilder) => endpointBuilder
         .NamedPipeEndpointTransport()
         .NewtonsoftSerializer()
         .ParticipateIn(_registry);

      public void ConfigureDomainDatabase(ExactlyOnceEndpointBuilder endpointBuilder)
      {
         endpointBuilder.Registrar.Register(Singleton.For<IConfigurationParameterProvider>()
                                                     .CreatedBy(() => new SqliteDatabasePerConnectionStringNameConfigurationParameterProvider(_workDirectory)));
         endpointBuilder.SqliteDomainDatabase(EndpointHostProcessEndpointIdentity.Name);
      }
   }

   ///<summary>The environment of the database-less composition: named pipes, Newtonsoft serialization, and participation in<br/>
   /// the shared registry — no database, no configuration, nothing persisted anywhere in this process.</summary>
   class DatabaselessEnvironment : IEndpointEnvironment
   {
      readonly InterprocessEndpointRegistry _registry;

      internal DatabaselessEnvironment(InterprocessEndpointRegistry registry) => _registry = registry;

      public void Configure(EndpointBuilder endpointBuilder) => endpointBuilder
         .NamedPipeEndpointTransport()
         .NewtonsoftSerializer()
         .ParticipateIn(_registry);

      public void ConfigureDomainDatabase(ExactlyOnceEndpointBuilder endpointBuilder) {}
   }

   ///<summary>The name of the environment variable through which the specification that launches this process passes the directory<br/>
   /// holding the NCrunch runtime assemblies — its own base directory. Set only when the specification runs under NCrunch.</summary>
   public const string NCrunchRuntimeAssemblyDirectoryVariableName = "COMPZE_NCRUNCH_RUNTIME_ASSEMBLY_DIRECTORY";

   ///<summary>NCrunch coverage-instruments the assemblies it builds, making their code call into NCrunch runtime assemblies — which are<br/>
   /// not part of this process's dependency closure, so the first instrumented dependency to execute would die failing to load them.<br/>
   /// The specification that launches this process passes the directory holding those assemblies through<br/>
   /// <see cref="NCrunchRuntimeAssemblyDirectoryVariableName"/>; outside NCrunch the variable is absent and this method installs nothing.</summary>
   static void MakeNCrunchInstrumentedDependenciesLoadable()
   {
      var nCrunchRuntimeAssemblyDirectory = Environment.GetEnvironmentVariable(NCrunchRuntimeAssemblyDirectoryVariableName);
      if(nCrunchRuntimeAssemblyDirectory == null) return;
      AssemblyLoadContext.Default.Resolving += (loadContext, assemblyName) =>
      {
         var candidate = Path.Combine(nCrunchRuntimeAssemblyDirectory, assemblyName.Name + ".dll");
         return File.Exists(candidate) ? loadContext.LoadFromAssemblyPath(candidate) : null;
      };
   }

   ///<summary>Serves every requested connection string as a sqlite database file named after it in this process's work directory —<br/>
   /// connection strings are the only configuration this composition reads.</summary>
   class SqliteDatabasePerConnectionStringNameConfigurationParameterProvider(DirectoryInfo workDirectory) : IConfigurationParameterProvider
   {
      readonly DirectoryInfo _workDirectory = workDirectory;

      public string GetString(string parameterName, string? valueIfMissing = null) =>
         $"Data Source={Path.Combine(_workDirectory.FullName, parameterName + ".sqlite")}";
   }
}
