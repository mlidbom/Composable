using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Composable.CQRS.EventHandling;
using Composable.DDD;
using Composable.DomainEvents;
using Composable.System;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing
{
    public class AggregateRootV2<TEntity, TBaseEventClass, TBaseEventInterface> : VersionedPersistentEntity<TEntity>, IEventStored, ISharedOwnershipAggregateRoot
        where TEntity : AggregateRootV2<TEntity, TBaseEventClass, TBaseEventInterface>
        where TBaseEventInterface : IAggregateRootEvent
        where TBaseEventClass : AggregateRootEvent, TBaseEventInterface
    {       
        //Yes empty. Id should be assigned by an action and it should be obvious that the aggregate in invalid until that happens
        protected AggregateRootV2() : base(Guid.Empty)
        {
            Contract.Assert(typeof(TBaseEventInterface).IsInterface && typeof(TBaseEventInterface) != typeof(IAggregateRootEvent));
        }

        private readonly IList<IAggregateRootEvent> _unCommittedEvents = new List<IAggregateRootEvent>();
        private readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TBaseEventInterface> _eventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TBaseEventInterface>();
        private readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TBaseEventInterface> _eventHandlersEventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TBaseEventInterface>();

        protected void RaiseEvent(TBaseEventClass theEvent)
        {
            theEvent.AggregateRootVersion = Version + 1;
            if(Version == 0)
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