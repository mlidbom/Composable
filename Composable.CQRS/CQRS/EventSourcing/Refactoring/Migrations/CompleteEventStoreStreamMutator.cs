using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System.Collections.Collections;

namespace Composable.CQRS.EventSourcing.Refactoring.Migrations
{
    internal abstract class CompleteEventStoreStreamMutator
    {
        public static ICompleteEventStreamMutator Create(IReadOnlyList<IEventMigration> eventMigrationFactories)
        {
            return eventMigrationFactories.Any()
                       ? new RealMutator(eventMigrationFactories)
                       : (ICompleteEventStreamMutator)new OnlySerializeVersionsMutator();
        }

        private class OnlySerializeVersionsMutator : ICompleteEventStreamMutator
        {
            private readonly Dictionary<Guid, int> _aggregateVersions = new Dictionary<Guid, int>();

            public IEnumerable<AggregateRootEvent> Mutate(IEnumerable<AggregateRootEvent> eventStream)
            {
                foreach(var @event in eventStream)
                {
                    var version = _aggregateVersions.GetOrAddDefault(@event.AggregateRootId) + 1;
                    _aggregateVersions[@event.AggregateRootId] = version;
                    @event.AggregateRootVersion = version;
                    yield return @event;
                }
            }
        }

        private class RealMutator : ICompleteEventStreamMutator
        {
            private readonly IReadOnlyList<IEventMigration> _eventMigrationFactories;
            private readonly Dictionary<Guid, ISingleAggregateInstanceEventStreamMutator> _aggregateMutatorsCache =
                new Dictionary<Guid, ISingleAggregateInstanceEventStreamMutator>();

            public RealMutator(IReadOnlyList<IEventMigration> eventMigrationFactories) { _eventMigrationFactories = eventMigrationFactories; }

            public IEnumerable<AggregateRootEvent> Mutate(IEnumerable<AggregateRootEvent> eventStream)
            {
                foreach(var @event in eventStream)
                {
                    var mutatedEvents = _aggregateMutatorsCache.GetOrAdd(
                        @event.AggregateRootId,
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
