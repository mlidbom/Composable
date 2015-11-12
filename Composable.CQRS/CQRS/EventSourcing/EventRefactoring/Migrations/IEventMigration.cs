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
        private IEnumerable<IAggregateRootEvent> _replacementEvents;
        private IEnumerable<IAggregateRootEvent> _insertedEvents;

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
            Contract.Assert(_replacementEvents == null, "You can only call Replace once");

            _replacementEvents = events;

            events.ForEach(
                (e, index) =>
                {
                    e.AggregateRootVersion = Event.AggregateRootVersion + index;
                    e.AggregateRootId = Event.AggregateRootId;
                });

            CurrentNode = CurrentNode.Replace(events).First();
        }

        public void InsertBefore(IEnumerable<IAggregateRootEvent> events)
        {
            Contract.Assert(_insertedEvents == null, "You can only call InsertBefore once");

            _insertedEvents = events;

            events.ForEach(
                (e, index) =>
                {
                    e.AggregateRootVersion = Event.AggregateRootVersion + index;
                    e.AggregateRootId = Event.AggregateRootId;
                });

            Event.AggregateRootVersion += events.Count();

            CurrentNode.AddBefore(events);
        }

        public IEnumerable<IAggregateRootEvent> MutatedHistory => _events.ToList();

        public IEnumerable<EventModifier> GetHistory()
        {
            return _events.Nodes().Select(@eventNode => new EventModifier(@eventNode)).ToList();
        }
    }

    internal class EventStreamMutator
    {
        private readonly Guid _aggregateId;
        private readonly IEventMigration[] _eventMigrations;
        public EventStreamMutator(Guid aggregateId,  params IEventMigration[] eventMigrations)
        {
            _aggregateId = aggregateId;
            _eventMigrations = eventMigrations;
        }

        private IEnumerable<IAggregateRootEvent> Mutate(IAggregateRootEvent @event, IEventMigration migration)
        {
            var modifier = new EventModifier(@event);
            migration.InspectEvent(@event, modifier);
            return modifier.MutatedHistory;
        }

        public IEnumerable<IAggregateRootEvent> Mutate(IAggregateRootEvent @event)
        {
            Contract.Assert(_aggregateId == @event.AggregateRootId);

            var modifier = new EventModifier(@event);

            foreach(var migration in _eventMigrations)
            {
                foreach(var innerModifier in modifier.GetHistory())
                {
                    migration.InspectEvent(innerModifier.Event, innerModifier);
                }
            }

            return modifier.MutatedHistory;
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
