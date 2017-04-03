using System;
using System.Collections.Generic;
using Castle.DynamicProxy;
using Composable.Contracts;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Refactoring.Migrations;
using Composable.Persistence.EventStore.Refactoring.Naming;
using Composable.System.Configuration;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using Composable.UnitsOfWork;
using JetBrains.Annotations;

// ReSharper disable UnusedTypeParameter the type parameters allow non-ambiguous registrations in the container. They are in fact used.

namespace Composable.DependencyInjection.Persistence
{
    interface IEventStore<TSessionInterface, TReaderInterface> : IEventStore {}

    public static class SqlServerEventStoreRegistrationExtensions
    {
        interface IEventStoreUpdater<TSessionInterface, TReaderInterface> : IEventStoreUpdater {}

        class EventStore<TSessionInterface, TReaderInterface> : EventStore, IEventStore<TSessionInterface, TReaderInterface>
        {
            public EventStore(string connectionString,
                                       IEventStoreEventSerializer serializer,
                                       ISingleContextUseGuard usageGuard = null,
                                       EventCache cache = null,
                                       IEventNameMapper nameMapper = null,
                                       IEnumerable<IEventMigration> migrations = null) : base(connectionString, serializer, usageGuard, cache, nameMapper, migrations) {}
        }

        class InMemoryEventStore<TSessionInterface, TReaderInterface> : InMemoryEventStore, IEventStore<TSessionInterface, TReaderInterface>
        {
            public InMemoryEventStore(IEnumerable<IEventMigration> migrations = null) : base(migrations) {}
        }

        [UsedImplicitly] class EventStoreUpdater<TSessionInterface, TReaderInterface> : EventStoreUpdater, IEventStoreUpdater<TSessionInterface, TReaderInterface>
        {
            public EventStoreUpdater(IServiceBus bus,
                                     IEventStore<TSessionInterface, TReaderInterface> store,
                                     ISingleContextUseGuard usageGuard,
                                     IUtcTimeTimeSource timeSource) : base(bus, store, usageGuard, timeSource) {}
        }

        public static void RegisterSqlServerEventStore<TSessionInterface, TReaderInterface>(this IDependencyInjectionContainer @this,
                                                                                            string connectionName,
                                                                                            IEnumerable<IEventMigration> migrations = null)
            where TSessionInterface : IEventStoreUpdater
            where TReaderInterface : IEventStoreReader
        {
            Contract.Argument(() => connectionName)
                    .NotNullEmptyOrWhiteSpace();

            GeneratedLowLevelInterfaceInspector.InspectInterfaces(Seq.OfTypes<TSessionInterface, TReaderInterface>());

            var cache = new EventCache();

            if(@this.RunMode().IsTesting && @this.RunMode().Mode == TestingMode.InMemory)
            {
                @this.Register(Component.For<IEventStore<TSessionInterface, TReaderInterface>>()
                                               .UsingFactoryMethod(sl => new InMemoryEventStore<TSessionInterface, TReaderInterface>(migrations: migrations))
                                               .LifestyleSingleton());
            } else
            {
                @this.Register(Component.For<IEventStore<TSessionInterface, TReaderInterface>>()
                                        .UsingFactoryMethod(sl => new EventStore<TSessionInterface, TReaderInterface>(
                                                                                                                               connectionString: sl.Resolve<IConnectionStringProvider>()
                                                                                                                                                   .GetConnectionString(connectionName)
                                                                                                                                                   .ConnectionString,
                                                                                                                               serializer: sl.Resolve<IEventStoreEventSerializer>(),
                                                                                                                               migrations: migrations,
                                                                                                                               cache: cache))
                                        .LifestyleScoped());
            }

            @this.Register(Component.For<IEventStoreUpdater<TSessionInterface, TReaderInterface>, IUnitOfWorkParticipant>()
                                           .ImplementedBy<EventStoreUpdater<TSessionInterface, TReaderInterface>>()
                                           .LifestyleScoped());

            @this.Register(Component.For<TSessionInterface>(Seq.OfTypes<TReaderInterface>())
                                           .UsingFactoryMethod(locator => CreateProxyFor<TSessionInterface, TReaderInterface>(locator.Resolve<IEventStoreUpdater<TSessionInterface, TReaderInterface>>()))
                                           .LifestyleScoped());
        }

        static TSessionInterface CreateProxyFor<TSessionInterface, TReaderInterface>(IEventStoreUpdater updater)
            where TSessionInterface : IEventStoreUpdater
            where TReaderInterface : IEventStoreReader
        {
            var sessionType = EventStoreSessionProxyFactory<TSessionInterface, TReaderInterface>.ProxyType;
            return (TSessionInterface)Activator.CreateInstance(sessionType, new IInterceptor[] {}, updater);
        }

        //Using a generic class this way allows us to bypass any need for dictionary lookups or similar giving us excellent performance.
        static class EventStoreSessionProxyFactory<TSessionInterface, TReaderInterface>
            where TSessionInterface : IEventStoreUpdater
            where TReaderInterface : IEventStoreReader
        {
            internal static readonly Type ProxyType = new DefaultProxyBuilder().CreateInterfaceProxyTypeWithTargetInterface(interfaceToProxy: typeof(IEventStoreUpdater),
                                                                                                                            additionalInterfacesToProxy: new[]
                                                                                                                                                         {
                                                                                                                                                             typeof(TSessionInterface),
                                                                                                                                                             typeof(TReaderInterface)
                                                                                                                                                         },
                                                                                                                            options: ProxyGenerationOptions.Default);
        }
    }
}
