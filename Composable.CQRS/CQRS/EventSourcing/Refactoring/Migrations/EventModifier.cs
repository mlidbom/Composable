using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.CQRS.EventSourcing;
using Composable.System.Collections.Collections;
using Composable.System.Linq;

namespace Composable.CQRS.CQRS.EventSourcing.Refactoring.Migrations
{
    //Yes this class has quite a bit of code that looks overly complex. Like it could be simplified a lot.
    //What you are seeing is likely optimizations. Please do not change this class for reasons of readability unless you do thorough performance testing and verify that no performance is lost.
    //The performance of this class is extremely important since it is called at least once for every event that is loaded from the event store when you have any migrations activated. It is called A LOT.
    //This is one of those central classes for which optimization is actually vitally important.
    //Each of the optimizations were done with the help of a profiler and running benchmarks on the tested performance improvements time and time again.
    class EventModifier : IEventModifier
    {
        readonly Action<IReadOnlyList<AggregateRootEvent>> _eventsAddedCallback;
        internal LinkedList<AggregateRootEvent> Events;
        AggregateRootEvent[] _replacementEvents;
        AggregateRootEvent[] _insertedEvents;

        public EventModifier(Action<IReadOnlyList<AggregateRootEvent>> eventsAddedCallback) => _eventsAddedCallback = eventsAddedCallback;

        public AggregateRootEvent Event;

        LinkedListNode<AggregateRootEvent> _currentNode;
        AggregateRootEvent _lastEventInActualStream;

        LinkedListNode<AggregateRootEvent> CurrentNode
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

        void AssertNoPriorModificationsHaveBeenMade()
        {
            Contract.Assert(_replacementEvents == null && _insertedEvents == null, "You can only modify the current event once.");
        }

        public void Replace(params AggregateRootEvent[] events)
        {
            AssertNoPriorModificationsHaveBeenMade();
            Contract.Assert(Event.GetType() != typeof(EndOfAggregateHistoryEventPlaceHolder), "You cannot call replace on the event that signifies the end of the stream");

            _replacementEvents = events;

            _replacementEvents.ForEach(
                (e, index) =>
                {
                    e.ManualVersion = e.AggregateRootVersion = Event.AggregateRootVersion + index;
                    e.Replaces = Event.InsertionOrder;
                    e.AggregateRootId = Event.AggregateRootId;
                    e.UtcTimeStamp = Event.UtcTimeStamp;
                });

            CurrentNode = CurrentNode.Replace(_replacementEvents);
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
            AssertNoPriorModificationsHaveBeenMade();

            _insertedEvents = insert;

            if(Event.GetType() == typeof(EndOfAggregateHistoryEventPlaceHolder))
            {
                _insertedEvents.ForEach(
                    (e, index) =>
                    {
                        e.InsertAfter = _lastEventInActualStream.InsertionOrder;
                        e.ManualVersion = e.AggregateRootVersion = Event.AggregateRootVersion + index;
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
                        e.ManualVersion = e.AggregateRootVersion = Event.AggregateRootVersion + index;
                        e.AggregateRootId = Event.AggregateRootId;
                        e.UtcTimeStamp = Event.UtcTimeStamp;
                    });
            }

            CurrentNode.ValuesFrom().ForEach((@event, index) => { @event.AggregateRootVersion += _insertedEvents.Length; });

            CurrentNode.AddBefore(_insertedEvents);
            _eventsAddedCallback.Invoke(_insertedEvents);
        }

        internal AggregateRootEvent[] MutatedHistory => Events?.ToArray() ?? new[] { Event };
    }
}