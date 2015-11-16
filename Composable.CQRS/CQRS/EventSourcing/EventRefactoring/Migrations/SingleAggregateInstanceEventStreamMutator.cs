using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.System.Collections.Collections;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing.EventRefactoring.Migrations
{
    //Yes this class has quite a bit of code that looks overly complex. Like it could be simplified a lot. 
    //What you are seeing is likely optimizations. Please do not change this class for reasons of readability unless you do thorough performance testing and verify that no performance is lost.
    //The performance of this class is extremely important since it is called at least once for every event that is loaded from the event store when you have any migrations activated. It is called A LOT.
    //This is one of those central classes for which optimization is actually vitally important.
    //Each of the optimizations were done with the help of a profiler and running benchmarks on the tested performance improvements time and time again.  
    internal class SingleAggregateInstanceEventStreamMutator : ISingleAggregateInstanceEventStreamMutator
    {
        private readonly Guid _aggregateId;
        private readonly ISingleAggregateInstanceEventMigrator[] _eventMigrations;
        private readonly EventModifier _eventModifier;

        private int _aggregateVersion = 1;

        public static ISingleAggregateInstanceEventStreamMutator Create(IAggregateRootEvent creationEvent, IReadOnlyList<IEventMigration> eventMigrations, Action<IReadOnlyList<AggregateRootEvent>> eventsAddedCallback = null)
        {
            return new SingleAggregateInstanceEventStreamMutator(creationEvent, eventMigrations, eventsAddedCallback);
        }

        private SingleAggregateInstanceEventStreamMutator
            (IAggregateRootEvent creationEvent, IEnumerable<IEventMigration> eventMigrations, Action<IReadOnlyList<AggregateRootEvent>> eventsAddedCallback)
        {
            _eventModifier = new EventModifier(eventsAddedCallback ?? (_ => { }));
            _aggregateId = creationEvent.AggregateRootId;
            _eventMigrations = eventMigrations
                .Where(migration => migration.MigratedAggregateEventHierarchyRootInterface.IsInstanceOfType(creationEvent))
                .Select(migration => migration.CreateMigrator())
                .ToArray();
        }

        public IEnumerable<AggregateRootEvent> Mutate(AggregateRootEvent @event)
        {
            Contract.Assert(_aggregateId == @event.AggregateRootId);
            if (_eventMigrations.Length == 0)
            {
                return Seq.Create(@event);
            }            

            @event.AggregateRootVersion = _aggregateVersion;
            _eventModifier.Reset(@event);

            for(var index = 0; index < _eventMigrations.Length; index++)
            {
                if (_eventModifier.Events == null)
                {
                    _eventMigrations[index].MigrateEvent(@event, _eventModifier);
                }
                else
                {
                    var node = _eventModifier.Events.First;
                    while (node != null)
                    {
                        _eventModifier.MoveTo(node);
                        _eventMigrations[index].MigrateEvent(_eventModifier.Event, _eventModifier);
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
            return Seq.Create(new EventStreamEndedEvent(_aggregateId, _aggregateVersion))
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
