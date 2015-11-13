using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing.EventRefactoring.Migrations
{
    internal class SingleAggregateInstanceEventStreamMutator : ISingleAggregateInstanceEventStreamMutator
    {
        private readonly Guid _aggregateId;
        private readonly IReadOnlyList<ISingleAggregateInstanceEventMigrator> _eventMigrations;
        private readonly Action<IReadOnlyList<IAggregateRootEvent>> _eventsAddedCallback;

        private int AggregateVersion { get; set; } = 1;

        public static ISingleAggregateInstanceEventStreamMutator Create(IAggregateRootEvent creationEvent, IReadOnlyList<IEventMigration> eventMigrations, Action<IReadOnlyList<IAggregateRootEvent>> eventsAddedCallback = null)
        {
            var pertinentMigrations = eventMigrations
                .Where(migration => migration.MigratedAggregateEventHierarchyRootInterface.IsInstanceOfType(creationEvent))
                .ToList();

            return pertinentMigrations.None()
                       ? NullOpMutator.Instance
                       : new SingleAggregateInstanceEventStreamMutator(creationEvent, eventMigrations, eventsAddedCallback);
        }

        private SingleAggregateInstanceEventStreamMutator
            (IAggregateRootEvent creationEvent, IEnumerable<IEventMigration> eventMigrations, Action<IReadOnlyList<IAggregateRootEvent>> eventsAddedCallback)
        {
            _eventsAddedCallback = eventsAddedCallback ?? (_ => {});
            _aggregateId = creationEvent.AggregateRootId;
            _eventMigrations = eventMigrations.Select(migration => migration.CreateMigrator()).ToList();
        }

        public IEnumerable<IAggregateRootEvent> Mutate(IAggregateRootEvent @event)
        {
            Contract.Assert(_aggregateId == @event.AggregateRootId);
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

        public static IEnumerable<IAggregateRootEvent> MutateCompleteAggregateHistory
            (IReadOnlyList<IEventMigration> eventMigrations,
             IReadOnlyList<IAggregateRootEvent> @events,
             Action<IReadOnlyList<IAggregateRootEvent>> eventsAddedCallback = null)
        {
            if (@eventMigrations.None())
            {
                return @events;
            }

            if(@events.None())
            {
                return Seq.Empty<IAggregateRootEvent>();
            }

            var mutator = Create(@events.First(), eventMigrations, eventsAddedCallback);
            return @events
                .SelectMany(mutator.Mutate)
                .Concat(mutator.EndOfAggregate())
                .ToList();
        }

        public IEnumerable<IAggregateRootEvent> EndOfAggregate()
        {
            return _eventMigrations.SelectMany(eventMigration => eventMigration.EndOfAggregateHistoryReached());
        }

        private class NullOpMutator : ISingleAggregateInstanceEventStreamMutator
        {
            public static readonly ISingleAggregateInstanceEventStreamMutator Instance = new NullOpMutator();
            public IEnumerable<IAggregateRootEvent> Mutate(IAggregateRootEvent @event) { yield return @event; }
            public IEnumerable<IAggregateRootEvent> EndOfAggregate() { return Seq.Empty<IAggregateRootEvent>(); }
        }
    }
}
