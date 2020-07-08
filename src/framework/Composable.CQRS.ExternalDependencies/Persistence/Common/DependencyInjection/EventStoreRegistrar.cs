using System;
using System.Collections.Generic;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.EventStore.Refactoring.Migrations;
using Composable.Persistence.InMemory.EventStore;
using Composable.Refactoring.Naming;
using Composable.Serialization;

namespace Composable.Persistence.Common.DependencyInjection
{
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

            @this.Register(Scoped.For<IEventStore>()
                                    .CreatedBy((IEventStorePersistenceLayer persistenceLayer, IEventStoreSerializer serializer, ITypeMapper typeMapper, EventCache eventCache) => new Persistence.EventStore.EventStore(persistenceLayer, typeMapper, serializer, eventCache, migrations)));

            @this.Register(Scoped.For<IEventStoreUpdater, IEventStoreReader>()
                                 .CreatedBy((IEventStoreEventPublisher eventPublisher, IEventStore eventStore, IUtcTimeTimeSource timeSource, IAggregateTypeValidator aggregateTypeValidator) =>
                                                new EventStoreUpdater(eventPublisher, eventStore, timeSource, aggregateTypeValidator)));

            return new EventStoreRegistrationBuilder();
        }

        internal static void RegisterEventStoreForFlexibleTesting(this IDependencyInjectionContainer @this,
                                                                  string connectionName,
                                                                  Func<IReadOnlyList<IEventMigration>> migrations)
        {
            Contract.Argument(connectionName, nameof(connectionName)).NotNullEmptyOrWhiteSpace();

            @this.Register(Singleton.For<EventCache>().CreatedBy(() => new EventCache()));

            @this.Register(Scoped.For<IEventStore>()
                                 .CreatedBy(
                                      (IEventStorePersistenceLayer persistenceLayer, ITypeMapper typeMapper, IEventStoreSerializer serializer, EventCache cache) =>
                                          new Persistence.EventStore.EventStore(
                                              persistenceLayer: persistenceLayer,
                                              typeMapper: typeMapper,
                                              serializer: serializer,
                                              migrations: migrations(),
                                              cache: cache)));

             @this.Register(Scoped.For<IEventStoreUpdater, IEventStoreReader>()
                                  .CreatedBy((IEventStoreEventPublisher eventPublisher, IEventStore eventStore, IUtcTimeTimeSource timeSource, IAggregateTypeValidator aggregateTypeValidator) =>
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
