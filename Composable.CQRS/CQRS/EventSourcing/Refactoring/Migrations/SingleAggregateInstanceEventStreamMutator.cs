using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.CQRS.EventSourcing;
using Composable.System.Linq;

// ReSharper disable ForCanBeConvertedToForeach

namespace Composable.CQRS.CQRS.EventSourcing.Refactoring.Migrations
{
    //Yes this class has quite a bit of code that looks overly complex. Like it could be simplified a lot.
    //What you are seeing is likely optimizations. Please do not change this class for reasons of readability unless you do thorough performance testing and verify that no performance is lost.
    //The performance of this class is extremely important since it is called at least once for every event that is loaded from the event store when you have any migrations activated. It is called A LOT.
    //This is one of those central classes for which optimization is actually vitally important.
    //Each of the optimizations were done with the help of a profiler and running benchmarks on the tested performance improvements time and time again.
    class SingleAggregateInstanceEventStreamMutator : ISingleAggregateInstanceEventStreamMutator
    {
        readonly Guid _aggregateId;
        readonly ISingleAggregateInstanceHandlingEventMigrator[] _eventMigrators;
        readonly EventModifier _eventModifier;

        int _aggregateVersion = 1;

        public static ISingleAggregateInstanceEventStreamMutator Create(IAggregateRootEvent creationEvent, IReadOnlyList<IEventMigration> eventMigrations, Action<IReadOnlyList<AggregateRootEvent>> eventsAddedCallback = null)
        {
            return new SingleAggregateInstanceEventStreamMutator(creationEvent, eventMigrations, eventsAddedCallback);
        }

        SingleAggregateInstanceEventStreamMutator
            (IAggregateRootEvent creationEvent, IEnumerable<IEventMigration> eventMigrations, Action<IReadOnlyList<AggregateRootEvent>> eventsAddedCallback)
        {
            _eventModifier = new EventModifier(eventsAddedCallback ?? (_ => { }));
            _aggregateId = creationEvent.AggregateRootId;
            _eventMigrators = eventMigrations
                .Where(migration => migration.MigratedAggregateEventHierarchyRootInterface.IsInstanceOfType(creationEvent))
                .Select(migration => migration.CreateSingleAggregateInstanceHandlingMigrator())
                .ToArray();
        }

        public IEnumerable<AggregateRootEvent> Mutate(AggregateRootEvent @event)
        {
            Contract.Assert(_aggregateId == @event.AggregateRootId);
            if (_eventMigrators.Length == 0)
            {
                return Seq.Create(@event);
            }

            @event.AggregateRootVersion = _aggregateVersion;
            _eventModifier.Reset(@event);

            for(var index = 0; index < _eventMigrators.Length; index++)
            {
                if (_eventModifier.Events == null)
                {
                    _eventMigrators[index].MigrateEvent(@event, _eventModifier);
                }
                else
                {
                    var node = _eventModifier.Events.First;
                    while (node != null)
                    {
                        _eventModifier.MoveTo(node);
                        _eventMigrators[index].MigrateEvent(_eventModifier.Event, _eventModifier);
                        node = node.Next;
                    }
                }
            }

            var newHistory = _eventModifier.MutatedHistory;
            _aggregateVersion += newHistory.Length;
            return newHistory;
        }

        public IEnumerable<AggregateRootEvent> EndOfAggregate()
        {
            return Seq.Create(new EndOfAggregateHistoryEventPlaceHolder(_aggregateId, _aggregateVersion))
                .SelectMany(Mutate)
                .Where(@event => @event.GetType() != typeof(EndOfAggregateHistoryEventPlaceHolder));
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

            var firstEvent = @events.First();
            lock(AggregateLockManager.GetAggregateLockObject(firstEvent.AggregateRootId))
            {

                var mutator = Create(@events.First(), eventMigrations, eventsAddedCallback);

                var result = @events
                    .SelectMany(mutator.Mutate)
                    .Concat(mutator.EndOfAggregate())
                    .ToArray();

                AssertMigrationsAreIdempotent(eventMigrations, result);

                return result;
            }
        }

        public static void AssertMigrationsAreIdempotent(IReadOnlyList<IEventMigration> eventMigrations, AggregateRootEvent[] @events)
        {
            var creationEvent = events.First();

            var migrators = eventMigrations
                .Where(migration => migration.MigratedAggregateEventHierarchyRootInterface.IsInstanceOfType(creationEvent))
                .Select(migration => migration.CreateSingleAggregateInstanceHandlingMigrator())
                .ToArray();

            for(var eventIndex = 0; eventIndex < events.Length; eventIndex++)
            {
                var @event = events[eventIndex];
                for(var migratorIndex = 0; migratorIndex < migrators.Length; migratorIndex++)
                {
                    migrators[migratorIndex].MigrateEvent(@event, AssertMigrationsAreIdempotentEventModifier.Instance);
                }
            }
        }
    }

    class EndOfAggregateHistoryEventPlaceHolder : AggregateRootEvent {
        public EndOfAggregateHistoryEventPlaceHolder(Guid aggregateId, int i):base(aggregateId)
        {
            AggregateRootVersion = i;
        }
    }
}
