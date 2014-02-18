using System;
using System.Collections.Generic;
using Composable.CQRS.EventHandling;
using Composable.DDD;
using Composable.DomainEvents;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing
{
    public class AggregateRoot<TEntity, TBaseEvent> : VersionedPersistentEntity<TEntity>, IEventStored, ISharedOwnershipAggregateRoot
        where TEntity : AggregateRoot<TEntity, TBaseEvent>
        where TBaseEvent : IAggregateRootEvent
    {       
        //Yes empty. Id should be assigned by an action and it should be obvious that the aggregate in invalid until that happens
        protected AggregateRoot():base(Guid.Empty) { }

        private readonly IList<IAggregateRootEvent> _unCommittedEvents = new List<IAggregateRootEvent>();
        private readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TBaseEvent> _eventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TBaseEvent>();

        protected void RaiseEvent(TBaseEvent theEvent)
        {
            theEvent.AggregateRootVersion = ++Version;
            if (!(theEvent is IAggregateRootCreatedEvent))
            {
                theEvent.AggregateRootId = Id;
            }
            ApplyEvent(theEvent);
            AssertInvariantsAreMet();
            _unCommittedEvents.Add(theEvent);
            DomainEvent.Raise(theEvent);
        }

        protected CallMatchingHandlersInRegistrationOrderEventDispatcher<TBaseEvent>.RegistrationBuilder RegisterEventAppliers()
        {
            return _eventDispatcher.RegisterHandlers();
        }

        private void ApplyEvent(TBaseEvent theEvent)
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
            history.ForEach(theEvent => ApplyEvent((TBaseEvent)theEvent));
        }

        void ISharedOwnershipAggregateRoot.IntegrateExternallyRaisedEvent(IAggregateRootEvent theEvent)
        {
            RaiseEvent((TBaseEvent)theEvent);
        }
    }
}