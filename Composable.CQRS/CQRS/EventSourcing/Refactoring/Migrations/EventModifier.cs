using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.System.Collections.Collections;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing.Refactoring.Migrations
{
    //Yes this class has quite a bit of code that looks overly complex. Like it could be simplified a lot. 
    //What you are seeing is likely optimizations. Please do not change this class for reasons of readability unless you do thorough performance testing and verify that no performance is lost.
    //The performance of this class is extremely important since it is called at least once for every event that is loaded from the event store when you have any migrations activated. It is called A LOT.
    //This is one of those central classes for which optimization is actually vitally important.
    //Each of the optimizations were done with the help of a profiler and running benchmarks on the tested performance improvements time and time again.  
    internal class EventModifier : IEventModifier
    {
        private readonly Action<IReadOnlyList<AggregateRootEvent>> _eventsAddedCallback;
        internal LinkedList<AggregateRootEvent> Events;
        private IReadOnlyList<AggregateRootEvent> _replacementEvents;
        private IReadOnlyList<AggregateRootEvent> _insertedEvents;

        public EventModifier(AggregateRootEvent @event, Action<IReadOnlyList<AggregateRootEvent>> eventsAddedCallback)
        {
            Event = @event;
            _eventsAddedCallback = eventsAddedCallback;
        }

        public EventModifier(Action<IReadOnlyList<AggregateRootEvent>> eventsAddedCallback)
        {
            _eventsAddedCallback = eventsAddedCallback;
        }

        public AggregateRootEvent Event;

        private LinkedListNode<AggregateRootEvent> _currentNode;
        private LinkedListNode<AggregateRootEvent> CurrentNode
        {
            get
            {
                if (Events == null)
                {
                    Events = new LinkedList<AggregateRootEvent>();
                    _currentNode = Events.AddFirst(Event);
                }
                return _currentNode;
            }
            set
            {
                _currentNode = value;
                Event = _currentNode.Value;
            }
        }

        public void Replace(IReadOnlyList<AggregateRootEvent> events)
        {
            Contract.Assert(_replacementEvents == null, $"You can only call {nameof(Replace)} once");
            Contract.Assert(Event.GetType() != typeof(EndOfAggregateHistoryEventPlaceHolder), "You cannot call replace on the event that signifies the end of the stream");

            _replacementEvents = events;

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

        public void Reset(AggregateRootEvent @event)
        {
            Event = @event;
            Events = null;
            _insertedEvents = null;
            _replacementEvents = null;
        }

        public void MoveTo(LinkedListNode<AggregateRootEvent> current)
        {
            CurrentNode = current;
            _insertedEvents = null;
            _replacementEvents = null;
        }

        public void InsertBefore(IReadOnlyList<AggregateRootEvent> insert)
        {
            Contract.Assert(_insertedEvents == null, $"You can only call {nameof(InsertBefore)} once");

            _insertedEvents = insert;

            _insertedEvents.ForEach(
                (e, index) =>
                {
                    e.InsertBefore = Event.InsertionOrder;
                    e.AggregateRootVersion = Event.AggregateRootVersion + index;
                    e.AggregateRootId = Event.AggregateRootId;
                });

            if (Event.GetType() == typeof(EndOfAggregateHistoryEventPlaceHolder))
            {
                //Review:mlidbo: Do some more thinking about this. Should we insert after the previous event? Will this always give the expected behaviour for the client implementing the migrator?
                _insertedEvents.ForEach(@event => @event.InsertBefore = null);//We are at the end of the stream. Claiming to insert before it makes no sense
            }

            CurrentNode.ValuesFrom().ForEach((@event, index) => @event.AggregateRootVersion += _insertedEvents.Count);

            CurrentNode.AddBefore(_insertedEvents);
            _eventsAddedCallback.Invoke(_insertedEvents);
        }

        internal AggregateRootEvent[] MutatedHistory => Events?.ToArray() ?? new[] { Event };
    }
}