using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.System.Collections.Collections;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing.EventRefactoring.Migrations
{
    internal class EventModifier : IEventModifier
    {
        private readonly Action<IReadOnlyList<AggregateRootEvent>> _eventsAddedCallback;
        private readonly LinkedList<AggregateRootEvent> _events;
        private List<AggregateRootEvent> _replacementEvents;
        private List<AggregateRootEvent> _insertedEvents;

        public EventModifier(AggregateRootEvent @event, Action<IReadOnlyList<AggregateRootEvent>> eventsAddedCallback)
        {
            _eventsAddedCallback = eventsAddedCallback;
            _events = new LinkedList<AggregateRootEvent>();
            CurrentNode = _events.AddFirst(@event);
        }

        private EventModifier(LinkedListNode<AggregateRootEvent> currentNode, Action<IReadOnlyList<AggregateRootEvent>> eventsAddedCallback)
        {
            _eventsAddedCallback = eventsAddedCallback;
            CurrentNode = currentNode;
            _events = currentNode.List;
        }

        public AggregateRootEvent Event => CurrentNode.Value;

        private LinkedListNode<AggregateRootEvent> CurrentNode { get; set; }

        public void Replace(IEnumerable<AggregateRootEvent> events)
        {
            Contract.Assert(_replacementEvents == null, $"You can only call {nameof(Replace)} once");

            _replacementEvents = events.ToList();

            _replacementEvents.Cast<AggregateRootEvent>().ForEach(
                (e, index) =>
                {
                    e.AggregateRootVersion = Event.AggregateRootVersion + index;
                    e.Replaces = Event.InsertionOrder;
                    e.AggregateRootId = Event.AggregateRootId;
                });

            CurrentNode = CurrentNode.Replace(_replacementEvents).First();
            _eventsAddedCallback.Invoke(_replacementEvents);
        }

        public void InsertBefore(IEnumerable<AggregateRootEvent> insert)
        {
            Contract.Assert(_insertedEvents == null, $"You can only call {nameof(InsertBefore)} once");

            _insertedEvents = insert.ToList();

            _insertedEvents.Cast<AggregateRootEvent>().ForEach(
                (e, index) =>
                {
                    e.InsertBefore = Event.InsertionOrder;
                    e.AggregateRootVersion = Event.AggregateRootVersion + index;
                    e.AggregateRootId = Event.AggregateRootId;
                });

            CurrentNode.ValuesFrom().Cast<AggregateRootEvent>().ForEach((@event, index) => @event.AggregateRootVersion += _insertedEvents.Count);

            CurrentNode.AddBefore(_insertedEvents);
            _eventsAddedCallback.Invoke(_insertedEvents);
        }

        internal IReadOnlyList<AggregateRootEvent> MutatedHistory => _events.ToList();

        public IEnumerable<EventModifier> GetHistory() { return _events.Nodes().Select(@eventNode => new EventModifier(@eventNode, _eventsAddedCallback)).ToList(); }
    }
}