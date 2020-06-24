using System;
using System.Collections.Generic;
using Castle.DynamicProxy;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Refactoring.Migrations;
using Composable.Persistence.InMemory.EventStore;
using Composable.Persistence.SqlServer.Configuration;
using Composable.Persistence.SqlServer.EventStore;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.System.Linq;
using Composable.System.Reflection;
using JetBrains.Annotations;

// ReSharper disable UnusedTypeParameter the type parameters allow non-ambiguous registrations in the container. They are in fact used.

namespace Composable.Persistence.Common.DependencyInjection
{
    interface IEventStore<TSessionInterface, TReaderInterface> : IEventStore {}

    //urgent: Remove persistence layer registration from this class.
    public static class EventStoreRegistrar
    {
        interface IEventStoreUpdater<TSessionInterface, TReaderInterface> : IEventStoreUpdater {}

        class EventStore<TSessionInterface, TReaderInterface> : Persistence.EventStore.EventStore, IEventStore<TSessionInterface, TReaderInterface>
        {
            public EventStore(IEventStoreSerializer serializer,
                              IEventStorePersistenceLayer persistenceLayer,
                              EventCache<TSessionInterface> cache,
                              IEnumerable<IEventMigration> migrations) : base(persistenceLayer, serializer, cache, migrations:migrations) {}
        }

        class InMemoryEventStore<TSessionInterface, TReaderInterface> : InMemoryEventStore, IEventStore<TSessionInterface, TReaderInterface>
        {
            public InMemoryEventStore(IEnumerable<IEventMigration> migrations ) : base(migrations) {}
        }

        [UsedImplicitly] class EventStoreUpdater<TSessionInterface, TReaderInterface> : EventStoreUpdater, IEventStoreUpdater<TSessionInterface, TReaderInterface>
        {
            public EventStoreUpdater(IEventstoreEventPublisher eventPublisher,
                                     IEventStore<TSessionInterface, TReaderInterface> store,
                                     IUtcTimeTimeSource timeSource,
                                     IAggregateTypeValidator aggregateTypeValidator) : base(eventPublisher, store, timeSource, aggregateTypeValidator) {}
        }

        interface IEventStorePersistenceLayer<TUpdater> : IEventStorePersistenceLayer
        {
        }

        class EventStorePersistenceLayer<TUpdaterType> : IEventStorePersistenceLayer<TUpdaterType>
        {
            public EventStorePersistenceLayer(IEventStoreSchemaManager schemaManager, IEventStoreEventReader eventReader, IEventStoreEventWriter eventWriter)
            {
                SchemaManager = schemaManager;
                EventReader = eventReader;
                EventWriter = eventWriter;
            }
            public IEventStoreSchemaManager SchemaManager { get; }
            public IEventStoreEventReader EventReader { get; }
            public IEventStoreEventWriter EventWriter { get; }
        }

        [UsedImplicitly] internal class EventCache<TUpdaterType> : EventCache
        {}


        public static SqlServerEventStoreRegistrationBuilder RegisterSqlServerEventStore(this IEndpointBuilder @this) => @this.RegisterSqlServerEventStore(new List<IEventMigration>());
        public static SqlServerEventStoreRegistrationBuilder RegisterSqlServerEventStore(this IEndpointBuilder @this, IReadOnlyList<IEventMigration> migrations)
            => @this.Container.RegisterSqlServerEventStore(@this.Configuration.ConnectionStringName, migrations);

        public static SqlServerEventStoreRegistrationBuilder RegisterSqlServerEventStore(this IDependencyInjectionContainer @this,
                                                                                         string connectionName) => @this.RegisterSqlServerEventStore(connectionName, new List<IEventMigration>());
        public static SqlServerEventStoreRegistrationBuilder RegisterSqlServerEventStore(this IDependencyInjectionContainer @this,
                                                                                            string connectionName,
                                                                                            IReadOnlyList<IEventMigration> migrations)
        {
              Contract.Argument(connectionName, nameof(connectionName))
                    .NotNullEmptyOrWhiteSpace();

            migrations ??= new List<IEventMigration>();

            @this.Register(Singleton.For<EventCache>().CreatedBy(() => new EventCache()));

            if (@this.RunMode.IsTesting && @this.RunMode.TestingMode == TestingMode.InMemory)
            {
                //Urgent:Refactor: No InMemoryEventStore should exist, instead there should be an InMemoryEventStorePersistenceLayer
                @this.Register(Singleton.For<IEventStore>()
                                        .CreatedBy(() => new InMemoryEventStore(migrations: migrations))
                                        .DelegateToParentServiceLocatorWhenCloning());
            } else
            {
                //Urgent:Refactor: This is the extension point. Everything else here is probably identical for all persistence layers. Remove this from here and do it in RegisterSqlServerPersistenceLayer instead.
                @this.Register(
                    Singleton.For<IEventStorePersistenceLayer>()
                                .CreatedBy((ISqlServerConnectionProviderSource connectionProviderSource, ITypeMapper typeIdMapper) =>
                                                    {
                                                        var connectionProvider = connectionProviderSource.GetConnectionProvider(connectionName);
                                                        var connectionManager = new SqlServerEventStoreConnectionManager(connectionProvider);
                                                        var schemaManager = new SqlServerEventStoreSchemaManager(connectionProvider, typeIdMapper);
                                                        var eventReader = new SqlServerEventStoreEventReader(connectionManager, schemaManager);
                                                        var eventWriter = new SqlServerEventStoreEventWriter(connectionManager, schemaManager);
                                                        return new EventStorePersistenceLayer<IEventStoreUpdater>(schemaManager, eventReader, eventWriter);
                                                    }));


                @this.Register(Scoped.For<IEventStore>()
                                        .CreatedBy((IEventStorePersistenceLayer persistenceLayer, IEventStoreSerializer serializer, EventCache eventCache) => new Persistence.EventStore.EventStore(persistenceLayer, serializer, eventCache, migrations)));
            }

            @this.Register(Scoped.For<IEventStoreUpdater, IEventStoreReader>()
                                    .CreatedBy((IEventstoreEventPublisher eventPublisher, IEventStore eventStore, IUtcTimeTimeSource timeSource, IAggregateTypeValidator aggregateTypeValidator) =>
                                                            new EventStoreUpdater(eventPublisher, eventStore, timeSource, aggregateTypeValidator)));

            return new SqlServerEventStoreRegistrationBuilder();
        }

        public static void RegisterSqlServerEventStore<TSessionInterface, TReaderInterface>(this IDependencyInjectionContainer @this,
                                                                                            string connectionName)
            where TSessionInterface : class, IEventStoreUpdater
            where TReaderInterface : IEventStoreReader
            => @this.RegisterSqlServerEventStore<TSessionInterface, TReaderInterface>(connectionName, new List<IEventMigration>());

        public static void RegisterSqlServerEventStore<TSessionInterface, TReaderInterface>(this IDependencyInjectionContainer @this,
                                                                                            string connectionName,
                                                                                            IReadOnlyList<IEventMigration> migrations)
            where TSessionInterface : class, IEventStoreUpdater
            where TReaderInterface : IEventStoreReader
            => @this.RegisterSqlServerEventStoreForFlexibleTesting<TSessionInterface, TReaderInterface>(
                connectionName,
                migrations != null
                    ? (Func<IReadOnlyList<IEventMigration>>)(() => migrations)
                    : (() => EmptyMigrationsArray));

        static readonly IEventMigration[] EmptyMigrationsArray = new IEventMigration[0];
        internal static void RegisterSqlServerEventStoreForFlexibleTesting<TSessionInterface, TReaderInterface>(this IDependencyInjectionContainer @this,
                                                                                                                string connectionName,
                                                                                                                Func<IReadOnlyList<IEventMigration>> migrations)
            where TSessionInterface : class, IEventStoreUpdater
            where TReaderInterface : IEventStoreReader
        {
            Contract.Argument(connectionName, nameof(connectionName))
                    .NotNullEmptyOrWhiteSpace();
            migrations ??= (() => EmptyMigrationsArray);

            GeneratedLowLevelInterfaceInspector.InspectInterfaces(Seq.OfTypes<TSessionInterface, TReaderInterface>());


            @this.Register(Singleton.For<EventCache<TSessionInterface>>()
                                    .CreatedBy(() => new EventCache<TSessionInterface>()));

            if (@this.RunMode.IsTesting && @this.RunMode.TestingMode == TestingMode.InMemory)
            {
                @this.Register(Singleton.For<InMemoryEventStore<TSessionInterface, TReaderInterface>>()
                                        .CreatedBy(() => new InMemoryEventStore<TSessionInterface, TReaderInterface>(migrations: migrations()))
                                        .DelegateToParentServiceLocatorWhenCloning());

                @this.Register(Scoped.For<IEventStore<TSessionInterface, TReaderInterface>>()
                                        .CreatedBy((InMemoryEventStore<TSessionInterface, TReaderInterface> store) =>
                                                            {
                                                                store.TestingOnlyReplaceMigrations(migrations());
                                                                return store;
                                                            }));
            } else
            {
                @this.Register(
                    Singleton.For<IEventStorePersistenceLayer<TSessionInterface>>()
                                .CreatedBy((ISqlServerConnectionProviderSource connectionProviderSource, ITypeMapper typeIdMapper) =>
                                                    {
                                                        var connectionProvider = connectionProviderSource.GetConnectionProvider(connectionName);
                                                        var connectionManager = new SqlServerEventStoreConnectionManager(connectionProvider);
                                                        var schemaManager = new SqlServerEventStoreSchemaManager(connectionProvider, typeIdMapper);
                                                        var eventReader = new SqlServerEventStoreEventReader(connectionManager, schemaManager);
                                                        var eventWriter = new SqlServerEventStoreEventWriter(connectionManager, schemaManager);
                                                        return new EventStorePersistenceLayer<TSessionInterface>(schemaManager, eventReader, eventWriter);
                                                    }));


                @this.Register(Scoped.For<IEventStore<TSessionInterface, TReaderInterface>>()
                                        .CreatedBy(
                                            (IEventStorePersistenceLayer<TSessionInterface> persistenceLayer, IEventStoreSerializer serializer, EventCache<TSessionInterface> cache) =>
                                                new EventStore<TSessionInterface, TReaderInterface>(
                                                    persistenceLayer: persistenceLayer,
                                                    serializer: serializer,
                                                    migrations: migrations(),
                                                    cache: cache)));
            }

            @this.Register(Scoped.For<IEventStoreUpdater<TSessionInterface, TReaderInterface>>()
                                    .CreatedBy((IEventstoreEventPublisher eventPublisher, IEventStore<TSessionInterface, TReaderInterface> eventStore, IUtcTimeTimeSource timeSource, IAggregateTypeValidator aggregateTypeValidator) =>
                                                            new EventStoreUpdater<TSessionInterface, TReaderInterface>(eventPublisher, eventStore, timeSource, aggregateTypeValidator)));

            var sessionType = EventStoreSessionProxyFactory<TSessionInterface, TReaderInterface>.ProxyType;
            var constructor = (Func<IInterceptor[], IEventStoreUpdater, TSessionInterface>)Constructor.Compile.ForReturnType(sessionType).WithArguments<IInterceptor[], IEventStoreUpdater>();
            var emptyInterceptorArray = new IInterceptor[0];

            @this.Register(Scoped.For<TSessionInterface, TReaderInterface>()
                                    .CreatedBy(EventStoreSessionProxyFactory<TSessionInterface, TReaderInterface>.ProxyType, locator => constructor(emptyInterceptorArray, locator.Resolve<IEventStoreUpdater<TSessionInterface, TReaderInterface>>())));
        }

        //Using a generic class this way allows us to bypass any need for dictionary lookups or similar giving us excellent performance.
        static class EventStoreSessionProxyFactory<TSessionInterface, TReaderInterface>
            where TSessionInterface : IEventStoreUpdater
            where TReaderInterface : IEventStoreReader
        {
            internal static readonly Type ProxyType = new DefaultProxyBuilder().CreateInterfaceProxyTypeWithTargetInterface(
                interfaceToProxy: typeof(IEventStoreUpdater),
                additionalInterfacesToProxy: new[]
                                             {
                                                 typeof(TSessionInterface),
                                                 typeof(TReaderInterface)
                                             },
                options: ProxyGenerationOptions.Default);
        }
    }

    public class SqlServerEventStoreRegistrationBuilder
    {
        public SqlServerEventStoreRegistrationBuilder HandleAggregate<TAggregate, TEvent>(MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
            where TAggregate : IEventStored<TEvent>
            where TEvent : IAggregateEvent
        {
           EventStoreApi.RegisterHandlersForAggregate<TAggregate, TEvent>(registrar);
            return this;
        }
    }
}
