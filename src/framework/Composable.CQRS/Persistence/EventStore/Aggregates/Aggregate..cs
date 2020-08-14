using System;
using System.Collections.Generic;
using Composable.Contracts;
using Composable.DDD;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Events;
using Composable.SystemCE;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ReactiveCE;
using JetBrains.Annotations;

#pragma warning disable CA1033 // Interface methods should be callable by child types

namespace Composable.Persistence.EventStore.Aggregates
{
    [Obsolete("Only here to let things compile while inheritors migrate to the version with 5 type parameters")]
    public class Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent> : Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent, AggregateEvent<TAggregateEvent>, IAggregateEvent<TAggregateEvent>>
        where TAggregate : Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent>
        where TAggregateEvent : class, IAggregateEvent
        where TAggregateEventImplementation : AggregateEvent, TAggregateEvent
    {
        [Obsolete("Only for infrastructure", true)]
        protected Aggregate() : this(DateTimeNowTimeSource.Instance) {}
        protected Aggregate([NotNull] IUtcTimeTimeSource timeSource) : base(timeSource) {}
    }

    public partial class Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent, TWrapperEventImplementation, TWrapperEventInterface> : VersionedEntity<TAggregate>, IEventStored<TAggregateEvent>
        where TWrapperEventImplementation : TWrapperEventInterface
        where TWrapperEventInterface : IAggregateEvent<TAggregateEvent>
        where TAggregate : Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent, TWrapperEventImplementation, TWrapperEventInterface>
        where TAggregateEvent : class, IAggregateEvent
        where TAggregateEventImplementation : AggregateEvent, TAggregateEvent
    {
        protected IUtcTimeTimeSource TimeSource { get; private set; }

        static Aggregate() => AggregateTypeValidator<TAggregate, TAggregateEventImplementation, TAggregateEvent>.AssertStaticStructureIsValid();

        [Obsolete("Only for infrastructure", true)]
        protected Aggregate() : this(DateTimeNowTimeSource.Instance) {}

        //Yes empty. Id should be assigned by an action and it should be obvious that the aggregate in invalid until that happens
        protected Aggregate(IUtcTimeTimeSource timeSource) : base(Guid.Empty)
        {
            Assert.Argument.NotNull(timeSource);
            Contract.Assert.That(typeof(TAggregateEvent).IsInterface, "typeof(TAggregateEvent).IsInterface");
            TimeSource = timeSource;
            _eventHandlersDispatcher.Register().IgnoreUnhandled<TAggregateEvent>();
        }

        readonly List<IAggregateEvent> _unCommittedEvents = new List<IAggregateEvent>();
        readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateEvent> _eventAppliersDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateEvent>();
        readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateEvent> _eventHandlersDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateEvent>();

        int _reentrancyLevel;
        bool _applyingEvents;

        readonly List<TAggregateEventImplementation> _eventsPublishedDuringCurrentPublishCallIncludingReentrantCallsFromEventHandlers = new List<TAggregateEventImplementation>();
        protected TEvent Publish<TEvent>(TEvent theEvent) where TEvent : TAggregateEventImplementation
        {
            Contract.Assert.That(!_applyingEvents, "You cannot raise events from within event appliers");

            using(ScopedChange.Enter(onEnter:() => _reentrancyLevel++, onDispose: () => _reentrancyLevel--))
            {
                theEvent.AggregateVersion = Version + 1;
                theEvent.UtcTimeStamp = TimeSource.UtcNow;
                if(Version == 0)
                {
                    if(!(theEvent is IAggregateCreatedEvent)) throw new Exception($"The first published event {theEvent.GetType()} did not implement {nameof(IAggregateCreatedEvent)}. The first event an aggregate publishes must always implement {nameof(IAggregateCreatedEvent)}.");
                    if(theEvent.AggregateId == Guid.Empty) throw new Exception($"{nameof(IAggregateEvent.AggregateId)} was empty in {nameof(IAggregateCreatedEvent)}");
                    theEvent.AggregateVersion = 1;
                } else
                {
                    if(theEvent.AggregateId != Guid.Empty && theEvent.AggregateId != Id) throw new ArgumentOutOfRangeException($"Tried to raise event for Aggregated: {theEvent.AggregateId} from Aggregate with Id: {Id}.");
                    theEvent.AggregateId = Id;
                }

                ApplyEvent(theEvent);
                _unCommittedEvents.Add(theEvent);
                _eventsPublishedDuringCurrentPublishCallIncludingReentrantCallsFromEventHandlers.Add(theEvent);
                _eventHandlersDispatcher.Dispatch(theEvent);
            }

            if(_reentrancyLevel == 0)
            {
                AssertInvariantsAreMet(); //It is allowed to enter a temporarily invalid state that will be corrected by new events published by event handlers. So we only check invariants once this event has been fully published including reentrancy.
                foreach(var @event in _eventsPublishedDuringCurrentPublishCallIncludingReentrantCallsFromEventHandlers) _eventStream.OnNext(@event);
                _eventsPublishedDuringCurrentPublishCallIncludingReentrantCallsFromEventHandlers.Clear();
            }

            return theEvent;
        }

        protected IEventHandlerRegistrar<TAggregateEvent> RegisterEventAppliers() => _eventAppliersDispatcher.RegisterHandlers();

        // ReSharper disable once UnusedMember.Global todo: coverage
        protected IEventHandlerRegistrar<TAggregateEvent> RegisterEventHandlers() => _eventHandlersDispatcher.RegisterHandlers();

        void ApplyEvent(TAggregateEvent theEvent)
        {
            using(ScopedChange.Enter(onEnter: () => _applyingEvents = true, onDispose: () => _applyingEvents = false))
            {
                if(theEvent is IAggregateCreatedEvent)
                {
#pragma warning disable 618 // Review OK: This is the one place where we are quite sure that calling this obsolete method is correct.
                    SetIdBeVerySureYouKnowWhatYouAreDoing(theEvent.AggregateId);
#pragma warning restore 618
                }

                Version = theEvent.AggregateVersion;
                _eventAppliersDispatcher.Dispatch(theEvent);
            }
        }

        protected virtual void AssertInvariantsAreMet() {}

        readonly SimpleObservable<TAggregateEventImplementation> _eventStream = new SimpleObservable<TAggregateEventImplementation>();
        IObservable<IAggregateEvent> IEventStored.EventStream => _eventStream;

        public void Commit(Action<IReadOnlyList<IAggregateEvent>> commitEvents)
        {
            commitEvents(_unCommittedEvents);
            _unCommittedEvents.Clear();
        }

        void IEventStored.SetTimeSource(IUtcTimeTimeSource timeSource) { TimeSource = timeSource; }

        void IEventStored.LoadFromHistory(IEnumerable<IAggregateEvent> history)
        {
            if(Version != 0) throw new InvalidOperationException($"You can only call {nameof(IEventStored.LoadFromHistory)} on an empty Aggregate with {nameof(Version)} == 0");
            history.ForEach(theEvent => ApplyEvent((TAggregateEvent)theEvent));
            AssertInvariantsAreMet();
        }
    }
}
