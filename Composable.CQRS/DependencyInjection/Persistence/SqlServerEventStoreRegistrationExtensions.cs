using System;
using System.Collections.Generic;
using Castle.DynamicProxy;
using Composable.Contracts;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.MicrosoftSQLServer;
using Composable.Persistence.EventStore.Refactoring.Migrations;
using Composable.Persistence.EventStore.Refactoring.Naming;
using Composable.System.Configuration;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using Composable.UnitsOfWork;

namespace Composable.DependencyInjection.Persistence
{
    interface IEventStore<TSessionInterface, TReaderInterface> : IEventStore { }

    public static class SqlServerEventStoreRegistrationExtensions
    {
        interface IEventStoreSession<TSessionInterface, TReaderInterface> : IEventStoreSession { }

        class SqlServerEventStore<TSessionInterface, TReaderInterface> : SqlServerEventStore, IEventStore<TSessionInterface, TReaderInterface>
        {
            public SqlServerEventStore(string connectionString, ISingleContextUseGuard usageGuard = null, SqlServerEventStoreEventsCache cache = null, IEventNameMapper nameMapper = null, IEnumerable<IEventMigration> migrations = null) : base(connectionString, usageGuard, cache, nameMapper, migrations) {}
        }

        class InMemoryEventStore<TSessionInterface, TReaderInterface> : InMemoryEventStore, IEventStore<TSessionInterface, TReaderInterface>
        {
            public InMemoryEventStore(IEnumerable<IEventMigration> migrationFactories = null) : base(migrationFactories) {}
        }

        class EventStoreSession<TSessionInterface, TReaderInterface> : EventStoreSession, IEventStoreSession<TSessionInterface, TReaderInterface>
        {
            public EventStoreSession(IServiceBus bus, IEventStore<TSessionInterface, TReaderInterface> store, ISingleContextUseGuard usageGuard, IUtcTimeTimeSource timeSource) : base(bus, store, usageGuard, timeSource) {}
        }

        public static void RegisterSqlServerEventStore<TSessionInterface, TReaderInterface>(this IDependencyInjectionContainer @this,
                                                                                                                       string connectionName,
                                                                                                                       IEnumerable<IEventMigration> migrations = null)
            where TSessionInterface : IEventStoreSession
            where TReaderInterface : IEventStoreReader
        {
            Contract.Argument(() => connectionName)
                        .NotNullEmptyOrWhiteSpace();

            var newContainer = @this;
            var serviceLocator = newContainer.CreateServiceLocator();

            GeneratedLowLevelInterfaceInspector.InspectInterfaces(Seq.OfTypes<TSessionInterface, TReaderInterface>());

            migrations = migrations ?? new List<IEventMigration>(); //We don't want to get any old migrations array that might have been registered by someone else.

            var connectionString =  serviceLocator.Resolve<IConnectionStringProvider>().GetConnectionString(connectionName).ConnectionString;


            if(newContainer.IsTestMode)
            {
                newContainer.Register(CComponent.For<IEventStore<TSessionInterface, TReaderInterface>>()
                                                .UsingFactoryMethod(sl => new InMemoryEventStore<TSessionInterface, TReaderInterface>(migrationFactories: migrations))
                                                .LifestyleSingleton());
            } else
            {
                newContainer.Register(CComponent.For<IEventStore<TSessionInterface, TReaderInterface>>()
                                                .UsingFactoryMethod(sl => new SqlServerEventStore<TSessionInterface, TReaderInterface>(connectionString: connectionString, migrations: migrations))
                                                .LifestyleScoped());
            }


            newContainer.Register(CComponent.For<IEventStoreSession<TSessionInterface, TReaderInterface>, IUnitOfWorkParticipant>()
                                            .ImplementedBy<EventStoreSession<TSessionInterface, TReaderInterface>>()
                                            .LifestyleScoped());

            newContainer.Register(CComponent.For<TSessionInterface>(Seq.OfTypes<TReaderInterface>())
                                            .UsingFactoryMethod(locator =>CreateProxyFor<TSessionInterface, TReaderInterface>(locator.Resolve<IEventStoreSession<TSessionInterface, TReaderInterface>>()))
                                            .LifestyleScoped());
        }

        static TSessionInterface CreateProxyFor<TSessionInterface, TReaderInterface>(IEventStoreSession session)
            where TSessionInterface : IEventStoreSession
            where TReaderInterface : IEventStoreReader
        {
            var sessionType = EventStoreSessionProxyFactory<TSessionInterface, TReaderInterface>.ProxyType;
            return (TSessionInterface)Activator.CreateInstance(sessionType, new IInterceptor[] { }, session);
        }


        //Using a generic class this way allows us to bypass any need for dictionary lookups or similar giving us excellent performance.
        static class EventStoreSessionProxyFactory<TSessionInterface, TReaderInterface>
            where TSessionInterface : IEventStoreSession
            where TReaderInterface : IEventStoreReader
        {
            internal static readonly Type ProxyType = new DefaultProxyBuilder().CreateInterfaceProxyTypeWithTargetInterface(interfaceToProxy: typeof(IEventStoreSession),
                                                                                                              additionalInterfacesToProxy: new[]
                                                                                                                                           {
                                                                                                                                               typeof(TSessionInterface),
                                                                                                                                               typeof(TReaderInterface)
                                                                                                                                           },
                                                                                                              options: ProxyGenerationOptions.Default);
        }
    }
}
