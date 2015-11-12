using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing.EventRefactoring.Migrations
{
    internal class SingleAggregateEventStreamMutator
    {
        private readonly Guid _aggregateId;
        private readonly IEventMigration[] _eventMigrations;

        private int AggregateVersion { get; set; } = 1;
        public SingleAggregateEventStreamMutator(Guid aggregateId, params IEventMigration[] eventMigrations)
        {
            _aggregateId = aggregateId;
            _eventMigrations = eventMigrations;
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

        private IAggregateRootEvent[] EndOfAggregate()
        {
            //throw new NotImplementedException();
            return new IAggregateRootEvent[0];
        }
    }
}