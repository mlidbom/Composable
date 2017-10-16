using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.System.Collections.Collections;
using Composable.System.Linq;

namespace Composable.Persistence.EventStore.Refactoring.Migrations
{
    //Yes this class has quite a bit of code that looks overly complex. Like it could be simplified a lot.
    //What you are seeing is likely optimizations. Please do not change this class for reasons of readability unless you do thorough performance testing and verify that no performance is lost.
    //The performance of this class is extremely important since it is called at least once for every event that is loaded from the event store when you have any migrations activated. It is called A LOT.
    //This is one of those central classes for which optimization is actually vitally important.
    //Each of the optimizations were done with the help of a profiler and running benchmarks on the tested performance improvements time and time again.
    class EventModifier : IEventModifier
    {
        readonly Action<IReadOnlyList<DomainEvent>> _eventsAddedCallback;
        internal LinkedList<DomainEvent> Events;
        DomainEvent[] _replacementEvents;
        DomainEvent[] _insertedEvents;

        public EventModifier(Action<IReadOnlyList<DomainEvent>> eventsAddedCallback) => _eventsAddedCallback = eventsAddedCallback;

        public DomainEvent Event;

        LinkedListNode<DomainEvent> _currentNode;
        DomainEvent _lastEventInActualStream;

        LinkedListNode<DomainEvent> CurrentNode
        {
            get
            {
                if (Events == null)
                {
                    Events = new LinkedList<DomainEvent>();
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
            OldContract.Assert.That(_replacementEvents == null && _insertedEvents == null, "You can only modify the current event once.");
        }

        public void Replace(params DomainEvent[] events)
        {
            AssertNoPriorModificationsHaveBeenMade();
            OldContract.Assert.That(Event.GetType() != typeof(EndOfAggregateHistoryEventPlaceHolder), "You cannot call replace on the event that signifies the end of the stream");

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

        public void Reset(DomainEvent @event)
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

        public void MoveTo(LinkedListNode<DomainEvent> current)
        {
            if (current.Value is EndOfAggregateHistoryEventPlaceHolder && !(Event is EndOfAggregateHistoryEventPlaceHolder))
            {
                _lastEventInActualStream = Event;
            }
            CurrentNode = current;
            _insertedEvents = null;
            _replacementEvents = null;
        }

        public void InsertBefore(params DomainEvent[] insert)
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

            CurrentNode.ValuesFrom().ForEach((@event, index) => @event.AggregateRootVersion += _insertedEvents.Length);

            CurrentNode.AddBefore(_insertedEvents);
            _eventsAddedCallback.Invoke(_insertedEvents);
        }

        internal DomainEvent[] MutatedHistory => Events?.ToArray() ?? new[] { Event };
    }
}