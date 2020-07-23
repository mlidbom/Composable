using System;
using System.Collections.Generic;
using System.Linq;
using Composable.SystemCE.CollectionsCE;

namespace Composable.Persistence.EventStore.Refactoring.Migrations
{
    abstract class CompleteEventStoreStreamMutator
    {
        public static ICompleteEventStreamMutator Create(IReadOnlyList<IEventMigration> eventMigrationFactories) => eventMigrationFactories.Any()
                                                                                                                        ? new RealMutator(eventMigrationFactories)
                                                                                                                        : (ICompleteEventStreamMutator)new OnlySerializeVersionsMutator();

        class OnlySerializeVersionsMutator : ICompleteEventStreamMutator
        {
            readonly Dictionary<Guid, int> _aggregateVersions = new Dictionary<Guid, int>();

            public IEnumerable<AggregateEvent> Mutate(IEnumerable<AggregateEvent> eventStream)
            {
                foreach(var @event in eventStream)
                {
                    var version = _aggregateVersions.GetOrAddDefault(@event.AggregateId) + 1;
                    _aggregateVersions[@event.AggregateId] = version;
                    @event.AggregateVersion = version;
                    yield return @event;
                }
            }
        }

        class RealMutator : ICompleteEventStreamMutator
        {
            readonly IReadOnlyList<IEventMigration> _eventMigrationFactories;
            readonly Dictionary<Guid, ISingleAggregateInstanceEventStreamMutator> _aggregateMutatorsCache =
                new Dictionary<Guid, ISingleAggregateInstanceEventStreamMutator>();

            public RealMutator(IReadOnlyList<IEventMigration> eventMigrationFactories) => _eventMigrationFactories = eventMigrationFactories;

            public IEnumerable<AggregateEvent> Mutate(IEnumerable<AggregateEvent> eventStream)
            {
                foreach(var @event in eventStream)
                {
                    var mutatedEvents = _aggregateMutatorsCache.GetOrAdd(
                        @event.AggregateId,
                        () => SingleAggregateInstanceEventStreamMutator.Create(@event, _eventMigrationFactories)
                        ).Mutate(@event);

                    foreach(var mutatedEvent in mutatedEvents)
                    {
                        yield return mutatedEvent;
                    }
                }

                foreach (var mutator in _aggregateMutatorsCache)
                {
                    foreach (var finalEvent in mutator.Value.EndOfAggregate())
                    {
                        yield return finalEvent;
                    }
                }
            }
        }
    }
}
