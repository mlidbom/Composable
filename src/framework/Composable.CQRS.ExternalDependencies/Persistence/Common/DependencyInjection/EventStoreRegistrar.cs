using System;
using System.Collections.Generic;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Refactoring.Migrations;
using Composable.Persistence.InMemory.EventStore;
using Composable.Serialization;
using Composable.System.Linq;

// ReSharper disable UnusedTypeParameter the type parameters allow non-ambiguous registrations in the container. They are in fact used.

namespace Composable.Persistence.Common.DependencyInjection
{
    //urgent: Remove persistence layer registration from this class.
    public static class EventStoreRegistrar
    {
        static readonly IEventMigration[] EmptyMigrationsArray = new IEventMigration[0];

        public static EventStoreRegistrationBuilder RegisterEventStore(this IEndpointBuilder @this) => @this.RegisterEventStore(EmptyMigrationsArray);
        public static EventStoreRegistrationBuilder RegisterEventStore(this IEndpointBuilder @this, IReadOnlyList<IEventMigration> migrations)
            => @this.Container.RegisterEventStore(@this.Configuration.ConnectionStringName, migrations);

        public static EventStoreRegistrationBuilder RegisterEventStore(this IDependencyInjectionContainer @this,
                                                                                         string connectionName) => @this.RegisterEventStore(connectionName, EmptyMigrationsArray);
        public static EventStoreRegistrationBuilder RegisterEventStore(this IDependencyInjectionContainer @this,
                                                                                            string connectionName,
                                                                                            IReadOnlyList<IEventMigration> migrations)
        {
            Contract.Argument(connectionName, nameof(connectionName)).NotNullEmptyOrWhiteSpace();

            @this.Register(Singleton.For<EventCache>().CreatedBy(() => new EventCache()));

            if (@this.RunMode.TestingPersistenceLayer == PersistenceLayer.InMemory)
            {
                //Urgent: No InMemoryEventStore should exist, instead there should be an InMemoryEventStorePersistenceLayer
                @this.Register(Singleton.For<IEventStore>()
                                        .CreatedBy(() => new InMemoryEventStore(migrations: migrations))
                                        .DelegateToParentServiceLocatorWhenCloning());
            } else
            {
                @this.Register(Scoped.For<IEventStore>()
                                        .CreatedBy((IEventStorePersistenceLayer persistenceLayer, IEventStoreSerializer serializer, EventCache eventCache) => new Persistence.EventStore.EventStore(persistenceLayer, serializer, eventCache, migrations)));
            }

            @this.Register(Scoped.For<IEventStoreUpdater, IEventStoreReader>()
                                    .CreatedBy((IEventstoreEventPublisher eventPublisher, IEventStore eventStore, IUtcTimeTimeSource timeSource, IAggregateTypeValidator aggregateTypeValidator) =>
                                                            new EventStoreUpdater(eventPublisher, eventStore, timeSource, aggregateTypeValidator)));

            return new EventStoreRegistrationBuilder();
        }

        internal static void RegisterEventStoreForFlexibleTesting(this IDependencyInjectionContainer @this,
                                                                  string connectionName,
                                                                  Func<IReadOnlyList<IEventMigration>> migrations)
        {
            Contract.Argument(connectionName, nameof(connectionName)).NotNullEmptyOrWhiteSpace();

            @this.Register(Singleton.For<EventCache>().CreatedBy(() => new EventCache()));

            if (@this.RunMode.TestingPersistenceLayer == PersistenceLayer.InMemory)
            {
                //Urgent: No InMemoryEventStore should exist, instead there should be an InMemoryEventStorePersistenceLayer
                @this.Register(Singleton.For<InMemoryEventStore>()
                                        .CreatedBy(() => new InMemoryEventStore(migrations: migrations()))
                                        .DelegateToParentServiceLocatorWhenCloning());

                @this.Register(Scoped.For<IEventStore>()
                                        .CreatedBy((InMemoryEventStore store) =>
                                                            {
                                                                store.TestingOnlyReplaceMigrations(migrations());
                                                                return store;
                                                            }));
            } else
            {
                @this.Register(Scoped.For<IEventStore>()
                                        .CreatedBy(
                                            (IEventStorePersistenceLayer persistenceLayer, IEventStoreSerializer serializer, EventCache cache) =>
                                                new EventStore.EventStore(
                                                    persistenceLayer: persistenceLayer,
                                                    serializer: serializer,
                                                    migrations: migrations(),
                                                    cache: cache)));
            }

            @this.Register(Scoped.For<IEventStoreUpdater, IEventStoreReader>()
                                    .CreatedBy((IEventstoreEventPublisher eventPublisher, IEventStore eventStore, IUtcTimeTimeSource timeSource, IAggregateTypeValidator aggregateTypeValidator) =>
                                                            new EventStoreUpdater(eventPublisher, eventStore, timeSource, aggregateTypeValidator)));
        }
    }

    public class EventStoreRegistrationBuilder
    {
        public EventStoreRegistrationBuilder HandleAggregate<TAggregate, TEvent>(MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
            where TAggregate : IEventStored<TEvent>
            where TEvent : IAggregateEvent
        {
           EventStoreApi.RegisterHandlersForAggregate<TAggregate, TEvent>(registrar);
            return this;
        }
    }
}
