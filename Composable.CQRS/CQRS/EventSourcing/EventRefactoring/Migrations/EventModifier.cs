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
        private readonly Action<IReadOnlyList<IAggregateRootEvent>> _eventsAddedCallback;
        private readonly LinkedList<IAggregateRootEvent> _events;
        private List<IAggregateRootEvent> _replacementEvents;
        private List<IAggregateRootEvent> _insertedEvents;

        public EventModifier(IAggregateRootEvent @event, Action<IReadOnlyList<IAggregateRootEvent>> eventsAddedCallback)
        {
            _eventsAddedCallback = eventsAddedCallback;
            _events = new LinkedList<IAggregateRootEvent>();
            CurrentNode = _events.AddFirst(@event);
        }

        private EventModifier(LinkedListNode<IAggregateRootEvent> currentNode, Action<IReadOnlyList<IAggregateRootEvent>> eventsAddedCallback)
        {
            _eventsAddedCallback = eventsAddedCallback;
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
                    e.Replaces = Event.InsertionOrder;
                    e.AggregateRootId = Event.AggregateRootId;
                });

            CurrentNode = CurrentNode.Replace(_replacementEvents).First();
            _eventsAddedCallback.Invoke(_replacementEvents);
        }

        public void InsertBefore(IEnumerable<IAggregateRootEvent> insert)
        {
            Contract.Assert(_insertedEvents == null, $"You can only call {nameof(InsertBefore)} once");

            _insertedEvents = insert.ToList();

            _insertedEvents.ForEach(
                (e, index) =>
                {
                    e.InsertBefore = Event.InsertionOrder;
                    e.AggregateRootVersion = Event.AggregateRootVersion + index;
                    e.AggregateRootId = Event.AggregateRootId;
                });

            CurrentNode.ValuesFrom().ForEach((@event, index) => @event.AggregateRootVersion += _insertedEvents.Count);

            CurrentNode.AddBefore(_insertedEvents);
            _eventsAddedCallback.Invoke(_insertedEvents);
        }

        internal IReadOnlyList<IAggregateRootEvent> MutatedHistory => _events.ToList();

        public IEnumerable<EventModifier> GetHistory() { return _events.Nodes().Select(@eventNode => new EventModifier(@eventNode, _eventsAddedCallback)).ToList(); }
    }
}