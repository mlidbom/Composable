using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing.EventRefactoring.Migrations
{
    internal class SingleAggregateEventStreamMutator : ISingleAggregateEventStreamMutator
    {
        private readonly Guid _aggregateId;
        private readonly IEventMigration[] _eventMigrations;

        private int AggregateVersion { get; set; } = 1;

        public static ISingleAggregateEventStreamMutator Create(Guid aggregateId, IReadOnlyList<Func<IEventMigration>> eventMigrations)
        {
            return @eventMigrations.None()
                       ? NullOpMutator.Instance
                       : new SingleAggregateEventStreamMutator(aggregateId, eventMigrations);
        }

        private SingleAggregateEventStreamMutator(Guid aggregateId, IEnumerable<Func<IEventMigration>> eventMigrations)
            : this(aggregateId, eventMigrations.Select(factory => factory())) { }

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

        public static IEnumerable<IAggregateRootEvent> MutateCompleteAggregateHistory
            (IReadOnlyList<Func<IEventMigration>> eventMigrations,
             IReadOnlyList<IAggregateRootEvent> @events)
        {
            if (@eventMigrations.None())
            {
                return @events;
            }

            if(@events.None())
            {
                return Seq.Empty<IAggregateRootEvent>();
            }

            var mutator = Create(@events.First().AggregateRootId, eventMigrations);
            return @events
                .SelectMany(mutator.Mutate)
                .Concat(mutator.EndOfAggregate())
                .ToList();
        }

        public IEnumerable<IAggregateRootEvent> EndOfAggregate() { return new IAggregateRootEvent[0]; }

        private class NullOpMutator : ISingleAggregateEventStreamMutator
        {
            public static readonly ISingleAggregateEventStreamMutator Instance = new NullOpMutator();
            public IEnumerable<IAggregateRootEvent> Mutate(IAggregateRootEvent @event) { yield return @event; }
            public IEnumerable<IAggregateRootEvent> EndOfAggregate() { return Seq.Empty<IAggregateRootEvent>(); }
        }
    }
}
