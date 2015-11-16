using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.System.Collections.Collections;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing.EventRefactoring.Migrations
{
    internal class SingleAggregateInstanceEventStreamMutator : ISingleAggregateInstanceEventStreamMutator
    {
        private readonly Guid _aggregateId;
        private readonly IReadOnlyList<ISingleAggregateInstanceEventMigrator> _eventMigrations;
        private readonly Action<IReadOnlyList<AggregateRootEvent>> _eventsAddedCallback;

        private int AggregateVersion { get; set; } = 1;

        public static ISingleAggregateInstanceEventStreamMutator Create(IAggregateRootEvent creationEvent, IReadOnlyList<IEventMigration> eventMigrations, Action<IReadOnlyList<AggregateRootEvent>> eventsAddedCallback = null)
        {
            return new SingleAggregateInstanceEventStreamMutator(creationEvent, eventMigrations, eventsAddedCallback);
        }

        private SingleAggregateInstanceEventStreamMutator
            (IAggregateRootEvent creationEvent, IEnumerable<IEventMigration> eventMigrations, Action<IReadOnlyList<AggregateRootEvent>> eventsAddedCallback)
        {
            _eventsAddedCallback = eventsAddedCallback ?? (_ => {});
            _aggregateId = creationEvent.AggregateRootId;
            _eventMigrations = eventMigrations
                .Where(migration => migration.MigratedAggregateEventHierarchyRootInterface.IsInstanceOfType(creationEvent))
                .Select(migration => migration.CreateMigrator())
                .ToList();
        }

        public IEnumerable<AggregateRootEvent> Mutate(AggregateRootEvent @event)
        {
            Contract.Assert(_aggregateId == @event.AggregateRootId);
            if (_eventMigrations.Count == 0)
            {
                return Seq.Create(@event);
            }            

            @event.AggregateRootVersion = AggregateVersion;
            var modifier = new EventModifier(@event, _eventsAddedCallback);

            foreach(var migration in _eventMigrations)
            {
                foreach(var innerModifier in modifier.GetHistory())
                {
                    migration.MigrateEvent(innerModifier.Event, innerModifier);
                }
            }

            var newHistory = modifier.MutatedHistory;
            AggregateVersion += newHistory.Count;
            return newHistory;
        }

        public IEnumerable<AggregateRootEvent> EndOfAggregate()
        {
            return Seq.Create(new EventStreamEndedEvent(_aggregateId, AggregateVersion))
                .SelectMany(Mutate)
                .Where(@event => @event.GetType() != typeof(EventStreamEndedEvent));
        }

        public static IReadOnlyList<AggregateRootEvent> MutateCompleteAggregateHistory
            (IReadOnlyList<IEventMigration> eventMigrations,
             IReadOnlyList<AggregateRootEvent> @events,
             Action<IReadOnlyList<AggregateRootEvent>> eventsAddedCallback = null)
        {
            if (eventMigrations.None())
            {
                return @events;
            }

            if(@events.None())
            {
                return Seq.Empty<AggregateRootEvent>().ToList();
            }

            var mutator = Create(@events.First(), eventMigrations, eventsAddedCallback);
            return @events
                .SelectMany(mutator.Mutate)
                .Concat(mutator.EndOfAggregate())
                .ToList();
        }              
    }

    internal class EventStreamEndedEvent : AggregateRootEvent {
        public EventStreamEndedEvent(Guid aggregateId, int i):base(aggregateId)
        {
            AggregateRootVersion = i;
        }
    }
}
