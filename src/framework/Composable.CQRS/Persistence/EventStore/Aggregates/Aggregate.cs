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
    public partial class Aggregate<TAggregate, TAggregateBaseEventClass, TAggregateBaseEventInterface> : VersionedPersistentEntity<TAggregate>, IEventStored
        where TAggregate : Aggregate<TAggregate, TAggregateBaseEventClass, TAggregateBaseEventInterface>
        where TAggregateBaseEventInterface : class, IAggregateRootEvent
        where TAggregateBaseEventClass : AggregateRootEvent, TAggregateBaseEventInterface
    {
        IUtcTimeTimeSource TimeSource { get; set; }

        static Aggregate() => AggregateTypeValidator<TAggregate, TAggregateBaseEventClass, TAggregateBaseEventInterface>.AssertStaticStructureIsValid();

        [Obsolete("Only for infrastructure", true)] protected Aggregate():this(DateTimeNowTimeSource.Instance){ }

        int _insertedVersionToAggregateVersionOffset = 0;

        //Yes empty. Id should be assigned by an action and it should be obvious that the aggregate in invalid until that happens
        protected Aggregate(IUtcTimeTimeSource timeSource) : base(Guid.Empty)
        {
            OldContract.Assert.That(timeSource != null, "timeSource != null");
            OldContract.Assert.That(typeof(TAggregateBaseEventInterface).IsInterface, "typeof(TAggregateBaseEventInterface).IsInterface");
            TimeSource = timeSource;
            _eventHandlersEventDispatcher.Register().IgnoreUnhandled<TAggregateBaseEventInterface>();
        }

        readonly IList<IAggregateRootEvent> _unCommittedEvents = new List<IAggregateRootEvent>();
        readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateBaseEventInterface> _eventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateBaseEventInterface>();
        readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateBaseEventInterface> _eventHandlersEventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateBaseEventInterface>();

        int _raiseEventReentrancyLevel = 0;
        List<TAggregateBaseEventClass> _raiseEventUnpushedEvents = new List<TAggregateBaseEventClass>();
        bool _applyingEvents;
        protected void Publish(TAggregateBaseEventClass theEvent)
        {
            OldContract.Assert.That(!_applyingEvents, "You cannot raise events from within event appliers");

            try
            {
                _raiseEventReentrancyLevel++;
                theEvent.AggregateRootVersion = Version + 1;
                theEvent.UtcTimeStamp = TimeSource.UtcNow;
                if(Version == 0)
                {
                    if(!(theEvent is IAggregateRootCreatedEvent))
                    {
                        throw new Exception($"The first raised event type {theEvent.GetType()} did not inherit {nameof(IAggregateRootCreatedEvent)}");
                    }
                    if(theEvent.AggregateRootId == Guid.Empty)
                    {
                        throw new Exception($"{nameof(IAggregateRootDeletedEvent.AggregateRootId)} was empty in {nameof(IAggregateRootCreatedEvent)}");
                    }
                    theEvent.AggregateRootVersion = 1;
                } else
                {
                    if(theEvent.AggregateRootId != Guid.Empty && theEvent.AggregateRootId != Id)
                    {
                        throw new ArgumentOutOfRangeException($"Tried to raise event for AggregateRootId: {theEvent.AggregateRootId} from AggregateRoot with Id: {Id}.");
                    }
                    if(_insertedVersionToAggregateVersionOffset != 0)
                    {
                        theEvent.InsertedVersion = theEvent.AggregateRootVersion + _insertedVersionToAggregateVersionOffset;
                        theEvent.ManualVersion = theEvent.AggregateRootVersion;
                    }
                    theEvent.AggregateRootId = Id;
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
        }

        protected IEventHandlerRegistrar<TAggregateBaseEventInterface> RegisterEventAppliers() => _eventDispatcher.RegisterHandlers();

        // ReSharper disable once UnusedMember.Global todo: coverage
        protected IEventHandlerRegistrar<TAggregateBaseEventInterface> RegisterEventHandlers() => _eventHandlersEventDispatcher.RegisterHandlers();

        void ApplyEvent(TAggregateBaseEventInterface theEvent)
        {
            try
            {
                _applyingEvents = true;
                if (theEvent is IAggregateRootCreatedEvent)
                {
                    SetIdBeVerySureYouKnowWhatYouAreDoing(theEvent.AggregateRootId);
                }
                Version = theEvent.AggregateRootVersion;
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

        readonly SimpleObservable<TAggregateBaseEventClass> _simpleObservable = new SimpleObservable<TAggregateBaseEventClass>();
        IObservable<IAggregateRootEvent> IEventStored.EventStream => _simpleObservable;

        void IEventStored.AcceptChanges()
        {
            _unCommittedEvents.Clear();
        }

        IEnumerable<IAggregateRootEvent> IEventStored.GetChanges() => _unCommittedEvents;

        void IEventStored.SetTimeSource(IUtcTimeTimeSource timeSource)
        {
            TimeSource = timeSource;
        }

        void IEventStored.LoadFromHistory(IEnumerable<IAggregateRootEvent> history)
        {
            history.ForEach(theEvent => ApplyEvent((TAggregateBaseEventInterface)theEvent));
            var maxInsertedVersion = history.Max(@event => ((AggregateRootEvent)@event).InsertedVersion);
            if(maxInsertedVersion != Version)
            {
                _insertedVersionToAggregateVersionOffset = maxInsertedVersion - Version;
            }
        }
    }
}