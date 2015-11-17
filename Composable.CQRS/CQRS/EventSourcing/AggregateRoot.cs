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
    public class AggregateRoot<TEntity, TBaseEventClass, TBaseEventInterface> : VersionedPersistentEntity<TEntity>, IEventStored, ISharedOwnershipAggregateRoot
        where TEntity : AggregateRoot<TEntity, TBaseEventClass, TBaseEventInterface>
        where TBaseEventInterface : IAggregateRootEvent
        where TBaseEventClass : AggregateRootEvent, TBaseEventInterface
    {
        protected internal ITimeSource TimeSource { get; private set; }

        [Obsolete("Only for infrastructure", true)]
        public AggregateRoot(){ }

        //Yes empty. Id should be assigned by an action and it should be obvious that the aggregate in invalid until that happens
        protected AggregateRoot(ITimeSource timeSource) : base(Guid.Empty)
        {
            Contract.Assert(timeSource != null);
            Contract.Assert(typeof(TBaseEventInterface).IsInterface);
            TimeSource = timeSource;            
        }

        private readonly IList<IAggregateRootEvent> _unCommittedEvents = new List<IAggregateRootEvent>();
        private readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TBaseEventInterface> _eventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TBaseEventInterface>();
        private readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TBaseEventInterface> _eventHandlersEventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TBaseEventInterface>();

        protected void RaiseEvent(TBaseEventClass theEvent)
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

        protected CallMatchingHandlersInRegistrationOrderEventDispatcher<TBaseEventInterface>.RegistrationBuilder RegisterEventAppliers()
        {
            return _eventDispatcher.RegisterHandlers();
        }

        protected CallMatchingHandlersInRegistrationOrderEventDispatcher<TBaseEventInterface>.RegistrationBuilder RegisterEventHandlers()
        {
            return _eventHandlersEventDispatcher.RegisterHandlers();
        }

        private void ApplyEvent(TBaseEventInterface theEvent)
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

        void IEventStored.AcceptChanges()
        {
            _unCommittedEvents.Clear();
        }

        IEnumerable<IAggregateRootEvent> IEventStored.GetChanges()
        {
            return _unCommittedEvents;
        }

        void IEventStored.SetTimeSource(ITimeSource timeSource)
        {
            TimeSource = timeSource;
        }

        void IEventStored.LoadFromHistory(IEnumerable<IAggregateRootEvent> history)
        {
            history.ForEach(theEvent => ApplyEvent((TBaseEventInterface)theEvent));
        }

        void ISharedOwnershipAggregateRoot.IntegrateExternallyRaisedEvent(IAggregateRootEvent theEvent)
        {
            RaiseEvent((TBaseEventClass)theEvent);
        }
    }
}