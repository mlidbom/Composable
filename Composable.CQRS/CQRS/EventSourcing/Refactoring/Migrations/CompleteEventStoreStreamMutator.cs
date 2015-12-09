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
                       : NullOpStreamMutator.Instance;
        }

        private class NullOpStreamMutator : ICompleteEventStreamMutator
        {
            public static readonly ICompleteEventStreamMutator Instance = new NullOpStreamMutator();

            public IEnumerable<AggregateRootEvent> Mutate(IEnumerable<AggregateRootEvent> eventStream) { return eventStream; }
        }

        private class RealMutator : ICompleteEventStreamMutator
        {
            private readonly IReadOnlyList<IEventMigration> _eventMigrationFactories;
            private readonly Dictionary<Guid, ISingleAggregateInstanceEventStreamMutator> _aggregateMutatorsCache =
                new Dictionary<Guid, ISingleAggregateInstanceEventStreamMutator>();

            public RealMutator(IReadOnlyList<IEventMigration> eventMigrationFactories) { _eventMigrationFactories = eventMigrationFactories; }

            public IEnumerable<AggregateRootEvent> Mutate(AggregateRootEvent @event)
            {
                return _aggregateMutatorsCache.GetOrAdd(
                    @event.AggregateRootId,
                    () => SingleAggregateInstanceEventStreamMutator.Create(@event, _eventMigrationFactories))
                                                .Mutate(@event);
            }

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
