using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DependencyInjection.SimpleInjectorImplementation;
using Composable.DependencyInjection.Windsor;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.AggregateRoots;
using Composable.Persistence.EventStore.Serialization.NewtonSoft;
using Composable.Refactoring.Naming;
using Composable.System.Configuration;
using Composable.System.Data.SqlClient;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;

namespace Composable.DependencyInjection.Testing
{
    static class TestingExtensions
    {
        static readonly ISqlConnection MasterDbConnection = new AppConfigSqlConnectionProvider().GetConnectionProvider(parameterName: "MasterDB");
        /// <summary>
        /// <para>SingleThreadUseGuard is registered for the component ISingleContextUseGuard</para>
        /// </summary>
        public static void ConfigureWiringForTestsCallBeforeAllOtherWiring(this IDependencyInjectionContainer @this, TestingMode mode = TestingMode.DatabasePool)
        {
            if(mode == TestingMode.DatabasePool)
            {
                MasterDbConnection.UseConnection(action: _ => {}); //evaluate lazy here in order to not pollute profiler timings of component resolution or registering.
            }

            @this.Register(
                Component.For<ISingleContextUseGuard>()
                         .ImplementedBy<SingleThreadUseGuard>()
                         .LifestyleScoped(),
                Component.For<IGlobalBusStateTracker>()
                         .UsingFactoryMethod(() => new GlobalBusStateTracker())
                         .LifestyleSingleton(),
                Component.For<IMessageHandlerRegistry, IMessageHandlerRegistrar, MessageHandlerRegistry>()
                         .UsingFactoryMethod((ITypeMapper typeMapper) => new MessageHandlerRegistry(typeMapper))
                         .LifestyleSingleton(),
                Component.For<ITypeMapper, ITypeMappingRegistar, TypeMapper>()
                         .ImplementedBy<TypeMapper>()
                         .LifestyleSingleton()
                         .DelegateToParentServiceLocatorWhenCloning(),
                Component.For<IAggregateTypeValidator>()
                         .ImplementedBy<AggregateTypeValidator>()
                         .LifestyleSingleton(),
                Component.For<IEventStoreEventSerializer>()
                         .ImplementedBy<NewtonSoftEventStoreEventSerializer>()
                         .LifestyleScoped(),
                Component.For<IUtcTimeTimeSource, DummyTimeSource>()
                         .UsingFactoryMethod(() => DummyTimeSource.Now)
                         .LifestyleSingleton()
                         .DelegateToParentServiceLocatorWhenCloning(),
                Component.For<IInProcessServiceBus>()
                         .UsingFactoryMethod((IMessageHandlerRegistry registry) => new InProcessServiceBus(registry))
                         .LifestyleSingleton(),
                Component.For<ISqlConnectionProvider>()
                         .UsingFactoryMethod(() => new SqlServerDatabasePoolSqlConnectionProvider(MasterDbConnection.ConnectionString))
                         .LifestyleSingleton()
                         .DelegateToParentServiceLocatorWhenCloning()
            );
        }

        static readonly IReadOnlyList<Type> TypesThatReferenceTheContainer = Seq.OfTypes<IDependencyInjectionContainer, IServiceLocator, SimpleInjectorDependencyInjectionContainer, WindsorDependencyInjectionContainer>()
                                                         .ToList();

        public static IServiceLocator Clone(this IServiceLocator @this)
        {
            var sourceContainer = (IDependencyInjectionContainer)@this;

            var cloneContainer = DependencyInjectionContainer.Create();

            sourceContainer.RegisteredComponents()
                           .Where(component => TypesThatReferenceTheContainer.None(type => component.ServiceTypes.Contains(type)))
                           .ForEach(action: componentRegistration => cloneContainer.Register(componentRegistration.CreateCloneRegistration(@this)));

            return cloneContainer.CreateServiceLocator();
        }
    }
}
