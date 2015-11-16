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
        private readonly ISingleAggregateInstanceEventMigrator[] _eventMigrations;
        private readonly Action<IReadOnlyList<AggregateRootEvent>> _eventsAddedCallback;
        private readonly EventModifier _eventModifier;

        private int AggregateVersion { get; set; } = 1;

        public static ISingleAggregateInstanceEventStreamMutator Create(IAggregateRootEvent creationEvent, IReadOnlyList<IEventMigration> eventMigrations, Action<IReadOnlyList<AggregateRootEvent>> eventsAddedCallback = null)
        {
            return new SingleAggregateInstanceEventStreamMutator(creationEvent, eventMigrations, eventsAddedCallback);
        }

        private SingleAggregateInstanceEventStreamMutator
            (IAggregateRootEvent creationEvent, IEnumerable<IEventMigration> eventMigrations, Action<IReadOnlyList<AggregateRootEvent>> eventsAddedCallback)
        {
            _eventsAddedCallback = eventsAddedCallback ?? (_ => {});
            _eventModifier = new EventModifier(_eventsAddedCallback);
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

            @event.AggregateRootVersion = AggregateVersion;
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
