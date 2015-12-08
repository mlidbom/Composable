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
        private AggregateRootEvent[] _replacementEvents;
        private AggregateRootEvent[] _insertedEvents;

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
        private AggregateRootEvent _lastEventInActualStream;

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

        public void Replace(params AggregateRootEvent[] events)
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
                    e.UtcTimeStamp = Event.UtcTimeStamp;
                });

            CurrentNode = CurrentNode.Replace(_replacementEvents).First();
            _eventsAddedCallback.Invoke(_replacementEvents);
        }

        public void Reset(AggregateRootEvent @event)
        {
            if(@event is EndOfAggregateHistoryEventPlaceHolder && !(Event is EndOfAggregateHistoryEventPlaceHolder))
            {
                _lastEventInActualStream = Event;
            }
            Event = @event;
            Events = null;
            _insertedEvents = null;
            _replacementEvents = null;
        }

        public void MoveTo(LinkedListNode<AggregateRootEvent> current)
        {
            if (current.Value is EndOfAggregateHistoryEventPlaceHolder && !(Event is EndOfAggregateHistoryEventPlaceHolder))
            {
                _lastEventInActualStream = Event;
            }
            CurrentNode = current;
            _insertedEvents = null;
            _replacementEvents = null;
        }

        public void InsertBefore(params AggregateRootEvent[] insert)
        {
            Contract.Assert(_insertedEvents == null, $"You can only call {nameof(InsertBefore)} once");

            _insertedEvents = insert;

            if(Event.GetType() == typeof(EndOfAggregateHistoryEventPlaceHolder))
            {
                _insertedEvents.ForEach(
                    (e, index) =>
                    {
                        e.InsertAfter = _lastEventInActualStream.InsertionOrder;
                        e.AggregateRootVersion = Event.AggregateRootVersion + index;
                        e.AggregateRootId = Event.AggregateRootId;
                        e.UtcTimeStamp = _lastEventInActualStream.UtcTimeStamp;
                    });                
            }
            else
            {
                _insertedEvents.ForEach(
                    (e, index) =>
                    {
                        e.InsertBefore = Event.InsertionOrder;
                        e.AggregateRootVersion = Event.AggregateRootVersion + index;
                        e.AggregateRootId = Event.AggregateRootId;
                        e.UtcTimeStamp = Event.UtcTimeStamp;
                    });
            }

            CurrentNode.ValuesFrom().ForEach((@event, index) => @event.AggregateRootVersion += _insertedEvents.Length);

            CurrentNode.AddBefore(_insertedEvents);
            _eventsAddedCallback.Invoke(_insertedEvents);
        }

        internal AggregateRootEvent[] MutatedHistory => Events?.ToArray() ?? new[] { Event };
    }
}