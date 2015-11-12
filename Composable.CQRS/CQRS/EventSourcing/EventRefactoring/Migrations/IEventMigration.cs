using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.System.Collections.Collections;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing.EventRefactoring.Migrations
{
    public interface IEventMigration
    {
        Guid Id { get; }
        string Name { get; }
        string Description { get; }
        bool Done { get; }

        ///<summary>Given the already seen history, insert any events at the end of the stream that might be required</summary>
        IEnumerable<IAggregateRootEvent> End();

        ///<summary>Inspect one event and if required mutate the event stream by replacing the event, or inserting event(s) before it</summary>
        void InspectEvent(IAggregateRootEvent @event, IEventModifier modifier);
    }

    public abstract class EventMigration : IEventMigration
    {
        public Guid Id { get; }
        public string Name { get; }
        public string Description { get; }
        public bool Done { get; }
        public virtual IEnumerable<IAggregateRootEvent> End() { return Seq.Empty<IAggregateRootEvent>(); }
        public abstract void InspectEvent(IAggregateRootEvent @event, IEventModifier modifier);
    }

    internal class EventModifier : IEventModifier
    {
        private readonly LinkedList<IAggregateRootEvent> _events;
        private List<IAggregateRootEvent> _replacementEvents;
        private List<IAggregateRootEvent> _insertedEvents;

        public EventModifier(IAggregateRootEvent @event)
        {
            _events = new LinkedList<IAggregateRootEvent>();
            CurrentNode = _events.AddFirst(@event);
        }

        private EventModifier(LinkedListNode<IAggregateRootEvent> currentNode)
        {
            CurrentNode = currentNode;
            _events = currentNode.List;
        }

        public IAggregateRootEvent Event => CurrentNode.Value;

        private LinkedListNode<IAggregateRootEvent> CurrentNode { get; set; }

        public void Replace(IEnumerable<IAggregateRootEvent> events)
        {
            Contract.Assert(_replacementEvents == null, $"You can only call {nameof(Replace)} once");

            _replacementEvents = events.ToList();

            _replacementEvents.ForEach(
                (e, index) =>
                {
                    e.AggregateRootVersion = Event.AggregateRootVersion + index;
                    e.AggregateRootId = Event.AggregateRootId;
                });

            CurrentNode = CurrentNode.Replace(_replacementEvents).First();
        }

        public void InsertBefore(IEnumerable<IAggregateRootEvent> insert)
        {
            Contract.Assert(_insertedEvents == null, $"You can only call {nameof(InsertBefore)} once");

            _insertedEvents = insert.ToList();

            _insertedEvents.ForEach(
                (e, index) =>
                {
                    e.AggregateRootVersion = Event.AggregateRootVersion + index;
                    e.AggregateRootId = Event.AggregateRootId;
                });

            CurrentNode.ValuesFrom().ForEach((@event, index) => @event.AggregateRootVersion += _insertedEvents.Count);

            CurrentNode.AddBefore(_insertedEvents);
        }

        internal IReadOnlyList<IAggregateRootEvent> MutatedHistory => _events.ToList();

        public IEnumerable<EventModifier> GetHistory() { return _events.Nodes().Select(@eventNode => new EventModifier(@eventNode)).ToList(); }
    }

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
