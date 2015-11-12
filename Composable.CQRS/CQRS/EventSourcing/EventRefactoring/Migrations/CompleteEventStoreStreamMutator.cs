using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System.Collections.Collections;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing.EventRefactoring.Migrations
{
    internal abstract class CompleteEventStoreStreamMutator
    {
        public static ICompleteEventStreamMutator Create(IReadOnlyList<Func<IEventMigration>> eventMigrationFactories)
        {
            return eventMigrationFactories.Any()
                       ? new RealMutator(eventMigrationFactories)
                       : NullOpStreamMutator.Instance;
        }

        private class NullOpStreamMutator : ICompleteEventStreamMutator
        {
            public static readonly ICompleteEventStreamMutator Instance = new NullOpStreamMutator();

            public IEnumerable<IAggregateRootEvent> Mutate(IEnumerable<IAggregateRootEvent> eventStream) { return eventStream; }
        }

        private class RealMutator : ICompleteEventStreamMutator
        {
            private readonly IReadOnlyList<Func<IEventMigration>> _eventMigrationFactories;
            private readonly Dictionary<Guid, ISingleAggregateEventStreamMutator> _aggregateMigrationsCache =
                new Dictionary<Guid, ISingleAggregateEventStreamMutator>();

            public RealMutator(IReadOnlyList<Func<IEventMigration>> eventMigrationFactories) { _eventMigrationFactories = eventMigrationFactories; }

            public IEnumerable<IAggregateRootEvent> Mutate(IAggregateRootEvent @event)
            {
                return _aggregateMigrationsCache.GetOrAdd(
                    @event.AggregateRootId,
                    () => SingleAggregateEventStreamMutator.Create(@event.AggregateRootId, _eventMigrationFactories))
                                                .Mutate(@event);
            }

            public IEnumerable<IAggregateRootEvent> Mutate(IEnumerable<IAggregateRootEvent> eventStream)
            {
                foreach(var @event in eventStream)
                {
                    var mutatedEvents = _aggregateMigrationsCache.GetOrAdd(
                        @event.AggregateRootId,
                        () => SingleAggregateEventStreamMutator.Create(@event.AggregateRootId, _eventMigrationFactories))
                                             .Mutate(@event);

                    foreach(var mutatedEvent in mutatedEvents)
                    {
                        yield return mutatedEvent;
                    }
                }

                foreach(var singleAggregateEventStreamMutator in _aggregateMigrationsCache.Values)
                {
                    foreach(var @event in singleAggregateEventStreamMutator.EndOfAggregate())
                    {
                        yield return @event;
                    }
                }
            }
        }
    }
}
