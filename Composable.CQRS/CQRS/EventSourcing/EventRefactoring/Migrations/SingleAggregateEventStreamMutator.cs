using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.System.Collections.Collections;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing.EventRefactoring.Migrations
{
    internal class SingleAggregateEventStreamMutator
    {
        private readonly Guid _aggregateId;
        private readonly IEventMigration[] _eventMigrations;

        private int AggregateVersion { get; set; } = 1;

        public SingleAggregateEventStreamMutator(Guid aggregateId, IEnumerable<Func<IEventMigration>> eventMigrations)
            : this(aggregateId, eventMigrations.Select(factory => factory())) {}

        private SingleAggregateEventStreamMutator(Guid aggregateId, IEnumerable<IEventMigration> eventMigrations)
        {
            _aggregateId = aggregateId;
            _eventMigrations = eventMigrations.ToArray();
        }

        public IEnumerable<IAggregateRootEvent> Mutate(IAggregateRootEvent @event)
        {
            Contract.Assert(_aggregateId == @event.AggregateRootId);
            @event.AggregateRootVersion = AggregateVersion;
            var modifier = new EventModifier(@event);

            foreach(var migration in _eventMigrations)
            {
                foreach(var innerModifier in modifier.GetHistory())
                {
                    migration.InspectEvent(innerModifier.Event, innerModifier);
                }
            }

            var newHistory = modifier.MutatedHistory;
            AggregateVersion += newHistory.Count;
            return newHistory;
        }

        public IEnumerable<IAggregateRootEvent> MutateCompleteAggregateHistory(IReadOnlyList<IAggregateRootEvent> @events)
        {
            return @events.SelectMany(Mutate).Append(EndOfAggregate());
        }

        internal IAggregateRootEvent[] EndOfAggregate()
        {
            //throw new NotImplementedException();
            return new IAggregateRootEvent[0];
        }
    }

    internal class CompleteEventStoreStreamMutator
    {
        private readonly IEnumerable<Func<IEventMigration>> _eventMigrationFactories;
        private Dictionary<Guid, SingleAggregateEventStreamMutator> _aggregateMigrationsCache =
            new Dictionary<Guid, SingleAggregateEventStreamMutator>();

        private int AggregateVersion { get; set; } = 1;

        public CompleteEventStoreStreamMutator(IEnumerable<Func<IEventMigration>> eventMigrationFactories)
        {
            _eventMigrationFactories = eventMigrationFactories;
        }

        public IEnumerable<IAggregateRootEvent> Mutate(IAggregateRootEvent @event)
        {
            return _aggregateMigrationsCache.GetOrAdd(
                @event.AggregateRootId,
                () => new SingleAggregateEventStreamMutator(@event.AggregateRootId, _eventMigrationFactories))
                                            .Mutate(@event);
        }

        private IEnumerable<IAggregateRootEvent> EndOfAggregate()
        {
            return _aggregateMigrationsCache.Values.SelectMany(mutator => mutator.EndOfAggregate());
        }
    }
}
