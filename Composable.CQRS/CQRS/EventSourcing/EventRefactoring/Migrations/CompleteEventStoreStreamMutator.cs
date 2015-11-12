using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System.Collections.Collections;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing.EventRefactoring.Migrations
{
    internal abstract class CompleteEventStoreStreamMutator
    {
        public static ICompleteEventStreamMutator Create(IEnumerable<Func<IEventMigration>> eventMigrationFactories)
        {
            if(eventMigrationFactories.Any())
            {
                return new RealMutator(eventMigrationFactories);
            }

            return NullOpStreamMutator.Instance;
        }

        private class NullOpStreamMutator : ICompleteEventStreamMutator
        {
            public static ICompleteEventStreamMutator Instance = new NullOpStreamMutator();
            public IEnumerable<IAggregateRootEvent> Mutate(IAggregateRootEvent @event) { yield return @event; }
            public IEnumerable<IAggregateRootEvent> EndOfStream() => Seq.Empty<IAggregateRootEvent>();
        }

        private class RealMutator : ICompleteEventStreamMutator
        {
            private readonly IEnumerable<Func<IEventMigration>> _eventMigrationFactories;
            private readonly Dictionary<Guid, ISingleAggregateEventStreamMutator> _aggregateMigrationsCache = new Dictionary<Guid, ISingleAggregateEventStreamMutator>();

            public RealMutator(IEnumerable<Func<IEventMigration>> eventMigrationFactories)
            {
                _eventMigrationFactories = eventMigrationFactories;
            }

            public IEnumerable<IAggregateRootEvent> Mutate(IAggregateRootEvent @event)
            {
                return _aggregateMigrationsCache.GetOrAdd(
                    @event.AggregateRootId,
                    () => SingleAggregateEventStreamMutator.Create(@event.AggregateRootId, _eventMigrationFactories))
                                                .Mutate(@event);
            }

            public IEnumerable<IAggregateRootEvent> EndOfStream()
            {
                return _aggregateMigrationsCache.Values.SelectMany(mutator => mutator.EndOfAggregate());
            }
        }
    }
}