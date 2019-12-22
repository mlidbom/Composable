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
    //Performance: Consider whether using the new stackalloc and Range types might allow us to improve performance of migrations.
    class EventModifier : IEventModifier
    {
        readonly Action<IReadOnlyList<AggregateEvent>> _eventsAddedCallback;
        internal LinkedList<AggregateEvent>? Events;
        AggregateEvent[]? _replacementEvents;
        AggregateEvent[]? _insertedEvents;

        public EventModifier(Action<IReadOnlyList<AggregateEvent>> eventsAddedCallback) => _eventsAddedCallback = eventsAddedCallback;

        AggregateEvent? _event;

        LinkedListNode<AggregateEvent>? _currentNode;
        AggregateEvent? _lastEventInActualStream;

        LinkedListNode<AggregateEvent> CurrentNode
        {
            get
            {
                if (_currentNode == null)
                {
                    Events = new LinkedList<AggregateEvent>();
                    _currentNode = Events.AddFirst(_event);
                }
                return _currentNode;
            }
            set
            {
                _currentNode = value;
                _event = _currentNode.Value;
            }
        }

        void AssertNoPriorModificationsHaveBeenMade()
        {
            if(_replacementEvents != null || _insertedEvents != null)
            {
                throw new Exception("You can only modify the current event once.");
            }

        }

        public void Replace(params AggregateEvent[] events)
        {
            AssertNoPriorModificationsHaveBeenMade();
            if(_event is EndOfAggregateHistoryEventPlaceHolder)
            {
                throw new Exception("You cannot call replace on the event that signifies the end of the stream");

            }

            _replacementEvents = events;

            _replacementEvents.ForEach(
                (e, index) =>
                {
                    e.ManualVersion = e.AggregateVersion = _event.AggregateVersion + index;
                    e.Replaces = _event.InsertionOrder;
                    e.AggregateId = _event.AggregateId;
                    e.UtcTimeStamp = _event.UtcTimeStamp;
                });

            CurrentNode = CurrentNode.Replace(_replacementEvents);
            _eventsAddedCallback.Invoke(_replacementEvents);
        }

        public void Reset(AggregateEvent @event)
        {
            if(@event is EndOfAggregateHistoryEventPlaceHolder && !(_event is EndOfAggregateHistoryEventPlaceHolder))
            {
                _lastEventInActualStream = _event;
            }
            _event = @event;
            Events = null;
            _currentNode = null;
            _insertedEvents = null;
            _replacementEvents = null;
        }

        public void MoveTo(LinkedListNode<AggregateEvent> current)
        {
            if (current.Value is EndOfAggregateHistoryEventPlaceHolder && !(_event is EndOfAggregateHistoryEventPlaceHolder))
            {
                _lastEventInActualStream = _event;
            }
            CurrentNode = current;
            _insertedEvents = null;
            _replacementEvents = null;
        }

        public void InsertBefore(params AggregateEvent[] insert)
        {
            AssertNoPriorModificationsHaveBeenMade();

            _insertedEvents = insert;

            if(_event is EndOfAggregateHistoryEventPlaceHolder)
            {
                _insertedEvents.ForEach(
                    (e, index) =>
                    {
                        e.InsertAfter = _lastEventInActualStream.InsertionOrder;
                        e.ManualVersion = e.AggregateVersion = _event.AggregateVersion + index;
                        e.AggregateId = _event.AggregateId;
                        e.UtcTimeStamp = _lastEventInActualStream.UtcTimeStamp;
                    });
            }
            else
            {
                _insertedEvents.ForEach(
                    (e, index) =>
                    {
                        e.InsertBefore = _event.InsertionOrder;
                        e.ManualVersion = e.AggregateVersion = _event.AggregateVersion + index;
                        e.AggregateId = _event.AggregateId;
                        e.UtcTimeStamp = _event.UtcTimeStamp;
                    });
            }

            CurrentNode.ValuesFrom().ForEach((@event, index) => @event.AggregateVersion += _insertedEvents.Length);

            CurrentNode.AddBefore(_insertedEvents);
            _eventsAddedCallback.Invoke(_insertedEvents);
        }

        internal AggregateEvent[] MutatedHistory => Events?.ToArray() ?? new[] { Assert.Result.NotNull(_event) };
    }
}