using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.DDD;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Events;
using Composable.System.Linq;
using Composable.System.Reactive;

namespace Composable.Persistence.EventStore.Aggregates
{
    public partial class Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent> : VersionedPersistentEntity<TAggregate>, IEventStored
        where TAggregate : Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent>
        where TAggregateEvent : class, IAggregateEvent
        where TAggregateEventImplementation : AggregateEvent, TAggregateEvent
    {
        IUtcTimeTimeSource TimeSource { get; set; }

        static Aggregate() => AggregateTypeValidator<TAggregate, TAggregateEventImplementation, TAggregateEvent>.AssertStaticStructureIsValid();

        [Obsolete("Only for infrastructure", true)] protected Aggregate():this(DateTimeNowTimeSource.Instance){ }

        int _insertedVersionToAggregateVersionOffset = 0;

        //Yes empty. Id should be assigned by an action and it should be obvious that the aggregate in invalid until that happens
        protected Aggregate(IUtcTimeTimeSource timeSource) : base(Guid.Empty)
        {
            Contract.Assert.That(timeSource != null, "timeSource != null");
            Contract.Assert.That(typeof(TAggregateEvent).IsInterface, "typeof(TAggregateEvent).IsInterface");
            TimeSource = timeSource;
            _eventHandlersEventDispatcher.Register().IgnoreUnhandled<TAggregateEvent>();
        }

        readonly IList<IAggregateEvent> _unCommittedEvents = new List<IAggregateEvent>();
        readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateEvent> _eventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateEvent>();
        readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateEvent> _eventHandlersEventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateEvent>();

        int _raiseEventReentrancyLevel = 0;
        List<TAggregateEventImplementation> _raiseEventUnpushedEvents = new List<TAggregateEventImplementation>();
        bool _applyingEvents;

        protected TEvent Publish<TEvent>(TEvent theEvent)
        where TEvent : TAggregateEventImplementation
        {
            Contract.Assert.That(!_applyingEvents, "You cannot raise events from within event appliers");

            try
            {
                _raiseEventReentrancyLevel++;
                theEvent.AggregateVersion = Version + 1;
                theEvent.UtcTimeStamp = TimeSource.UtcNow;
                if(Version == 0)
                {
                    if(!(theEvent is IAggregateCreatedEvent))
                    {
                        throw new Exception($"The first raised event type {theEvent.GetType()} did not inherit {nameof(IAggregateCreatedEvent)}");
                    }
                    if(theEvent.AggregateId == Guid.Empty)
                    {
                        throw new Exception($"{nameof(IAggregateDeletedEvent.AggregateId)} was empty in {nameof(IAggregateCreatedEvent)}");
                    }
                    theEvent.AggregateVersion = 1;
                } else
                {
                    if(theEvent.AggregateId != Guid.Empty && theEvent.AggregateId != Id)
                    {
                        throw new ArgumentOutOfRangeException($"Tried to raise event for Aggregated: {theEvent.AggregateId} from Aggregate with Id: {Id}.");
                    }
                    if(_insertedVersionToAggregateVersionOffset != 0)
                    {
                        theEvent.InsertedVersion = theEvent.AggregateVersion + _insertedVersionToAggregateVersionOffset;
                        theEvent.ManualVersion = theEvent.AggregateVersion;
                    }
                    theEvent.AggregateId = Id;
                }

                ApplyEvent(theEvent);
                AssertInvariantsAreMet();
                _unCommittedEvents.Add(theEvent);
                _raiseEventUnpushedEvents.Add(theEvent);
                _eventHandlersEventDispatcher.Dispatch(theEvent);
            }
            finally
            {
                _raiseEventReentrancyLevel--;
            }

            if(_raiseEventReentrancyLevel == 0)
            {
                foreach(var @event in _raiseEventUnpushedEvents)
                {
                    _simpleObservable.OnNext(@event);
                }
                _raiseEventUnpushedEvents.Clear();
            }

            return theEvent;
        }

        protected IEventHandlerRegistrar<TAggregateEvent> RegisterEventAppliers() => _eventDispatcher.RegisterHandlers();

        // ReSharper disable once UnusedMember.Global todo: coverage
        protected IEventHandlerRegistrar<TAggregateEvent> RegisterEventHandlers() => _eventHandlersEventDispatcher.RegisterHandlers();

        void ApplyEvent(TAggregateEvent theEvent)
        {
            try
            {
                _applyingEvents = true;
                if (theEvent is IAggregateCreatedEvent)
                {
                    SetIdBeVerySureYouKnowWhatYouAreDoing(theEvent.AggregateId);
                }
                Version = theEvent.AggregateVersion;
                _eventDispatcher.Dispatch(theEvent);
            }
            finally
            {
                _applyingEvents = false;
            }
        }

        protected virtual void AssertInvariantsAreMet()
        {
        }

        readonly SimpleObservable<TAggregateEventImplementation> _simpleObservable = new SimpleObservable<TAggregateEventImplementation>();
        IObservable<IAggregateEvent> IEventStored.EventStream => _simpleObservable;

        void IEventStored.AcceptChanges()
        {
            _unCommittedEvents.Clear();
        }

        IEnumerable<IAggregateEvent> IEventStored.GetChanges() => _unCommittedEvents;

        void IEventStored.SetTimeSource(IUtcTimeTimeSource timeSource)
        {
            TimeSource = timeSource;
        }

        void IEventStored.LoadFromHistory(IEnumerable<IAggregateEvent> history)
        {
            history.ForEach(theEvent => ApplyEvent((TAggregateEvent)theEvent));
            var maxInsertedVersion = history.Max(@event => ((AggregateEvent)@event).InsertedVersion);
            if(maxInsertedVersion != Version)
            {
                _insertedVersionToAggregateVersionOffset = maxInsertedVersion - Version;
            }
        }
    }
}