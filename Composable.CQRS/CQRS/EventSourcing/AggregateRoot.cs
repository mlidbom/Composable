using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Composable.CQRS.EventHandling;
using Composable.DDD;
using Composable.DomainEvents;
using Composable.GenericAbstractions.Time;
using Composable.System;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing
{
    public partial class AggregateRoot<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface> : VersionedPersistentEntity<TAggregateRoot>, IEventStored, ISharedOwnershipAggregateRoot
        where TAggregateRoot : AggregateRoot<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
        where TAggregateRootBaseEventInterface : class, IAggregateRootEvent
        where TAggregateRootBaseEventClass : AggregateRootEvent, TAggregateRootBaseEventInterface
    {
        protected IUtcTimeTimeSource TimeSource { get; private set; }

        [Obsolete("Only for infrastructure", true)]
        public AggregateRoot():this(DateTimeNowTimeSource.Instance){ }

        //Yes empty. Id should be assigned by an action and it should be obvious that the aggregate in invalid until that happens
        protected AggregateRoot(IUtcTimeTimeSource timeSource) : base(Guid.Empty)
        {
            Contract.Assert(timeSource != null);
            Contract.Assert(typeof(TAggregateRootBaseEventInterface).IsInterface);
            TimeSource = timeSource;
            _eventHandlersEventDispatcher.Register().IgnoreUnhandled<IAggregateRootEvent>();
        }

        private readonly IList<IAggregateRootEvent> _unCommittedEvents = new List<IAggregateRootEvent>();
        private readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateRootBaseEventInterface> _eventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateRootBaseEventInterface>();
        private readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateRootBaseEventInterface> _eventHandlersEventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateRootBaseEventInterface>();

        protected void RaiseEvent(TAggregateRootBaseEventClass theEvent)
        {
            theEvent.AggregateRootVersion = Version + 1;
            theEvent.UtcTimeStamp = TimeSource.UtcNow;
            if (Version == 0)
            {
                if(!theEvent.IsInstanceOf<IAggregateRootCreatedEvent>())
                {
                    throw new Exception($"The first raised event type {theEvent.GetType()} did not inherit {nameof(IAggregateRootCreatedEvent)}");
                }
                theEvent.AggregateRootVersion = 1;
            }else
            {
                if(theEvent.AggregateRootId != Guid.Empty && theEvent.AggregateRootId != Id)
                {
                    throw new ArgumentOutOfRangeException("Tried to raise event for AggregateRootId: {0} from AggregateRoot with Id: {1}."
                        .FormatWith(theEvent.AggregateRootId, Id));
                }
                theEvent.AggregateRootId = Id;
            }
            ApplyEvent(theEvent);
            AssertInvariantsAreMet();
            _unCommittedEvents.Add(theEvent);
            _eventHandlersEventDispatcher.Dispatch(theEvent);
            DomainEvent.Raise(theEvent);
        }

        protected CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateRootBaseEventInterface>.RegistrationBuilder RegisterEventAppliers()
        {
            return _eventDispatcher.RegisterHandlers();
        }

        protected CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateRootBaseEventInterface>.RegistrationBuilder RegisterEventHandlers()
        {
            return _eventHandlersEventDispatcher.RegisterHandlers();
        }

        private void ApplyEvent(TAggregateRootBaseEventInterface theEvent)
        {
            if (theEvent is IAggregateRootCreatedEvent)
            {
                SetIdBeVerySureYouKnowWhatYouAreDoing(theEvent.AggregateRootId);
            }
            Version = theEvent.AggregateRootVersion;
            _eventDispatcher.Dispatch(theEvent);
        }

        protected virtual void AssertInvariantsAreMet()
        {
        }

        IUtcTimeTimeSource IEventStored.TimeSource => TimeSource;

        void IEventStored.AcceptChanges()
        {
            _unCommittedEvents.Clear();
        }

        IEnumerable<IAggregateRootEvent> IEventStored.GetChanges()
        {
            return _unCommittedEvents;
        }

        void IEventStored.SetTimeSource(IUtcTimeTimeSource timeSource)
        {
            TimeSource = timeSource;
        }

        void IEventStored.LoadFromHistory(IEnumerable<IAggregateRootEvent> history)
        {
            history.ForEach(theEvent => ApplyEvent((TAggregateRootBaseEventInterface)theEvent));
        }

        void ISharedOwnershipAggregateRoot.IntegrateExternallyRaisedEvent(IAggregateRootEvent theEvent)
        {
            RaiseEvent((TAggregateRootBaseEventClass)theEvent);
        }
    }
}