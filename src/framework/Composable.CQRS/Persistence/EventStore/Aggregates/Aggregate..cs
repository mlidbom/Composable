using System;
using System.Collections.Generic;
using Composable.Contracts;
using Composable.DDD;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Events;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ReactiveCE;

#pragma warning disable CA1033 // Interface methods should be callable by child types

namespace Composable.Persistence.EventStore.Aggregates
{
    public partial class Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent> : VersionedEntity<TAggregate>, IEventStored<TAggregateEvent>
        where TAggregate : Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent>
        where TAggregateEvent : class, IAggregateEvent
        where TAggregateEventImplementation : AggregateEvent, TAggregateEvent
    {
        IUtcTimeTimeSource TimeSource { get; set; }

        static Aggregate() => AggregateTypeValidator<TAggregate, TAggregateEventImplementation, TAggregateEvent>.AssertStaticStructureIsValid();

        [Obsolete("Only for infrastructure", true)] protected Aggregate():this(DateTimeNowTimeSource.Instance){ }

        //Yes empty. Id should be assigned by an action and it should be obvious that the aggregate in invalid until that happens
        protected Aggregate(IUtcTimeTimeSource timeSource) : base(Guid.Empty)
        {
            Assert.Argument.NotNull(timeSource);
            Contract.Assert.That(typeof(TAggregateEvent).IsInterface, "typeof(TAggregateEvent).IsInterface");
            TimeSource = timeSource;
            _eventHandlersEventDispatcher.Register().IgnoreUnhandled<TAggregateEvent>();
        }

        readonly List<IAggregateEvent> _unCommittedEvents = new List<IAggregateEvent>();
        readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateEvent> _eventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateEvent>();
        readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateEvent> _eventHandlersEventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateEvent>();

        int _reentrancyLevel = 0;
        readonly List<TAggregateEventImplementation> _unpublishedEvents = new List<TAggregateEventImplementation>();
        bool _applyingEvents;

        protected TEvent Publish<TEvent>(TEvent theEvent)
        where TEvent : TAggregateEventImplementation
        {
            Contract.Assert.That(!_applyingEvents, "You cannot raise events from within event appliers");

            try
            {
                _reentrancyLevel++;
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
                    theEvent.AggregateId = Id;
                }

                ApplyEvent(theEvent);
                AssertInvariantsAreMet();
                _unCommittedEvents.Add(theEvent);
                _unpublishedEvents.Add(theEvent);
                _eventHandlersEventDispatcher.Dispatch(theEvent);
            }
            finally
            {
                _reentrancyLevel--;
            }

            if(_reentrancyLevel == 0)
            {
                foreach(var @event in _unpublishedEvents)
                {
                    _eventStream.OnNext(@event);
                }
                _unpublishedEvents.Clear();
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
#pragma warning disable 618 // Review OK: This is the one place where we are quite sure that calling this obsolete method is correct.
                    SetIdBeVerySureYouKnowWhatYouAreDoing(theEvent.AggregateId);
#pragma warning restore 618
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

        readonly SimpleObservable<TAggregateEventImplementation> _eventStream = new SimpleObservable<TAggregateEventImplementation>();
        IObservable<IAggregateEvent> IEventStored.EventStream => _eventStream;

        public void Commit(Action<IReadOnlyList<IAggregateEvent>> commitEvents)
        {
            commitEvents(_unCommittedEvents);
            _unCommittedEvents.Clear();
        }

        void IEventStored.SetTimeSource(IUtcTimeTimeSource timeSource)
        {
            TimeSource = timeSource;
        }

        void IEventStored.LoadFromHistory(IEnumerable<IAggregateEvent> history)
        {
            history.ForEach(theEvent => ApplyEvent((TAggregateEvent)theEvent));
            AssertInvariantsAreMet();
        }
    }
}