using Compze.Tessaging.TessageBus.Exceptions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Endpoints;
using Compze.Hosting;
using Compze.Hosting.SameMachine;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.SameMachine.EndpointHostProcess;
using Compze.Threading;
using Compze.Threading.Testing;
using Compze.xUnitMatrix;
//The unqualified name Program silently resolves to the entry point xUnit v3 generates into THIS test assembly, not to the endpoint host process's class.
using EndpointHostProcessProgram = Compze.Tests.SameMachine.EndpointHostProcess.Program;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

using Compze.TypeIdentifiers.DependencyInjection;

namespace Compze.Tests.Integration.SameMachine;

///<summary>The same-machine story end to end, across REAL process boundaries: a separate OS process hosts an endpoint over named<br/>
/// pipes, both processes discover each other through a shared <see cref="InterprocessEndpointRegistry"/>, and an exactly-once<br/>
/// tommand conversation crosses in both directions — no web stack, no database server, no configuration.</summary>
public class Given_a_separate_process_hosting_an_endpoint_discovered_through_a_shared_interprocess_registry : UniversalTestBase
{
   //The endpoint host process speaks named pipes; the conversation only makes sense when the specification's endpoint does too.
   static bool RunsOnTheNamedPipesTransport => TestEnv.Transport == Transport.NamedPipes;

   readonly DirectoryInfo _workDirectory = null!;
   readonly InterprocessEndpointRegistry _registry = null!;
   readonly EndpointHostProcessHandle _endpointHostProcess = null!;
   readonly IEndpointHost _specificationHost = null!;
   readonly ExactlyOnceEndpoint _specificationEndpoint = null!;
   readonly IThreadGate _replyTommandGate = IThreadGate.NewOpen(WaitTimeout.Seconds(1), "replyTommand");

   public Given_a_separate_process_hosting_an_endpoint_discovered_through_a_shared_interprocess_registry()
   {
      if(!RunsOnTheNamedPipesTransport) return;

      _workDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Compze", "Tests", "MultiProcess", Guid.NewGuid().ToString()));
      _workDirectory.Create();
      const string registryName = "EndpointRegistry";
      _registry = InterprocessEndpointRegistry.OpenOrCreateSessionLocal(registryName, _workDirectory);

      _endpointHostProcess = EndpointHostProcessHandle.Start(registryName, _workDirectory, EndpointHostProcessProgram.ExactlyOnceTessagingComposition);

      _specificationHost = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder()
                                                                       ._mutate(it => it.Registrar.CurrentTestsDbPoolIfNotCloneContainer()),
                                                          new EnvironmentParticipatingInTheSharedRegistry(_registry));
      _specificationEndpoint = _specificationHost.RegisterEndpoint(new SpecificationEndpointDeclaration(this));
   }

   ///<summary>The current test's transport and domain-database binding, plus participation in the shared interprocess<br/>
   /// registry the endpoint host process is discovered through.</summary>
   class EnvironmentParticipatingInTheSharedRegistry : IEndpointEnvironment
   {
      readonly InterprocessEndpointRegistry _registry;
      internal EnvironmentParticipatingInTheSharedRegistry(InterprocessEndpointRegistry registry) => _registry = registry;

      public void Configure(EndpointBuilder endpointBuilder)
      {
         endpointBuilder.TransportProtocol(registrar => registrar.CurrentTestsEndpointTransport());
         endpointBuilder.ParticipateIn(_registry);
      }

      public void ConfigureDomainDatabase(ExactlyOnceEndpointBuilder endpointBuilder) =>
         endpointBuilder.ConfigurePersistence(registrar => registrar.CurrentTestsConfiguredSqlLayer(connectionStringName: endpointBuilder.Configuration.Id.ToString()));
   }

   class SpecificationEndpointDeclaration : ExactlyOnceEndpointDeclaration<SpecificationEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "SpecificationEndpoint";
      public static EndpointId Id { get; } = new(Guid.Parse("C143AAD9-E33C-4A8A-A594-E75A8A125DE0"));

      readonly Given_a_separate_process_hosting_an_endpoint_discovered_through_a_shared_interprocess_registry _specification;
      internal SpecificationEndpointDeclaration(Given_a_separate_process_hosting_an_endpoint_discovered_through_a_shared_interprocess_registry specification) => _specification = specification;

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireMappedTypesFromAssemblyContaining<TommandSentToTheEndpointHostProcess>();

      protected override void RegisterExactlyOnceTommandHandlers(IExactlyOnceTommandHandlerRegistrar handle) => handle
         .ForTommand((TommandSentBackToTheSpecificationProcess _) =>
          {
             _specification._replyTommandGate.AwaitPassThrough();
             return Task.CompletedTask;
          });
   }

   protected override async Task InitializeAsyncInternal()
   {
      if(!RunsOnTheNamedPipesTransport) return;
      await _specificationHost.StartAsync();
   }

   protected override async Task DisposeAsyncInternal()
   {
      if(!RunsOnTheNamedPipesTransport) return;
      await _specificationHost.DisposeAsync();
      await _endpointHostProcess.DisposeAsync();
      _registry.Delete();
      _registry.Dispose();
      await DeleteWorkDirectoryRetryingWhileTheKilledProcessesFileLocksRelease();
   }

   ///<summary>The killed process's memory-mapped file sections can outlive its exit by a moment, so the recursive delete retries briefly.<br/>
   /// Cleaning the OS temp directory is hygiene, not an assertion: if a lock outlasts the retries the directory is left for the OS.</summary>
   async Task DeleteWorkDirectoryRetryingWhileTheKilledProcessesFileLocksRelease()
   {
      for(var attempt = 1; attempt <= 20; attempt++)
      {
         try
         {
            _workDirectory.Delete(recursive: true);
            return;
         }
         catch(IOException)
         {
            await Task.Delay(100);
         }
      }
   }

   [Skip<Transport>([Transport.AspNetCore], "The endpoint host process speaks named pipes; the conversation only makes sense when the specification's endpoint does too")]
   [PCT] public async Task a_tommand_sent_to_the_process_is_handled_there_and_its_reply_tommand_comes_back()
   {
      //The send is a waiting send: the tommand's handler has never been met until this endpoint's reconciliation discovers
      //the endpoint host process - which is still starting up - and the exactly-once cold-start bind waits that discovery out
      //within the endpoint's handler-availability patience, then binds. (The hand-rolled retry-on-no-handler loop this
      //replaced is exactly the pattern waiting sends exist to dissolve.) The catch only enriches an exhausted wait with the
      //process's fate and console output - the diagnosis a cross-process failure needs - and rethrows.
      try
      {
         await _specificationEndpoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new TommandSentToTheEndpointHostProcess());
      }
      catch(NoHandlerForTessageTypeException notDiscoveredWithinPatience)
      {
         _endpointHostProcess.ThrowDescribingTheFailureIfTheProcessHasExited();
         throw new InvalidOperationException($"The endpoint host process was not discovered within the endpoint's handler-availability patience.{Environment.NewLine}{_endpointHostProcess.ConsoleOutput}", notDiscoveredWithinPatience);
      }

      try
      {
         _replyTommandGate.AwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(30));
      }
      catch(Exception replyNeverArrived)
      {
         _endpointHostProcess.ThrowDescribingTheFailureIfTheProcessHasExited();
         //The tommand reached the endpoint host process (the waiting send above succeeded), so what went wrong is on ITS side -
         //its handler, its outbox, its delivery back to us - and only its console output tells that story.
         throw new InvalidOperationException($"The reply tommand never arrived, and the endpoint host process is still running.{Environment.NewLine}{_endpointHostProcess.ConsoleOutput}", replyNeverArrived);
      }
   }
}
