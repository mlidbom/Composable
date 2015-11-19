using System;
using System.Collections.Generic;
using Composable.CQRS.EventHandling;
using Composable.DDD;
using Composable.DomainEvents;
using Composable.GenericAbstractions.Time;
using Composable.System;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing
{
    [Obsolete("No longer supported. please use AggregateRoot<TEntity, TBaseEventClass, TBaseEventInterface>")]
    public class AggregateRoot<TEntity, TBaseEvent> : VersionedPersistentEntity<TEntity>, IEventStored, ISharedOwnershipAggregateRoot
        where TEntity : AggregateRoot<TEntity, TBaseEvent>
        where TBaseEvent : IAggregateRootEvent
    {

        protected AggregateRoot():this(DateTimeNowTimeSource.Instance) { }
        //Yes empty. Id should be assigned by an action and it should be obvious that the aggregate in invalid until that happens
        protected AggregateRoot(IUtcTimeTimeSource timeSource = null) : base(Guid.Empty)
        {
            TimeSource = timeSource ?? DateTimeNowTimeSource.Instance;
        }

        private readonly IList<IAggregateRootEvent> _unCommittedEvents = new List<IAggregateRootEvent>();
        private readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TBaseEvent> _eventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TBaseEvent>();

        protected void RaiseEvent(TBaseEvent theEvent)
        {
            ((AggregateRootEvent)(object)theEvent).UtcTimeStamp = TimeSource.UtcNow;
            ((AggregateRootEvent)(object)theEvent).AggregateRootVersion = ++Version;
            if (!(theEvent is IAggregateRootCreatedEvent))
            {
                if (theEvent.AggregateRootId != Guid.Empty && theEvent.AggregateRootId != Id)
                {
                    throw new ArgumentOutOfRangeException("Tried to raise event for AggregateRootId: {0} from AggregateRoot with Id: {1}."
                        .FormatWith(theEvent.AggregateRootId, Id));
                }
                ((AggregateRootEvent)(object)theEvent).AggregateRootId = Id;
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


        IUtcTimeTimeSource IEventStored.TimeSource => TimeSource;

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

        protected IUtcTimeTimeSource TimeSource { get; private set; }
        void IEventStored.SetTimeSource(IUtcTimeTimeSource timeSource)
        {
            TimeSource = timeSource;
        }

        void ISharedOwnershipAggregateRoot.IntegrateExternallyRaisedEvent(IAggregateRootEvent theEvent)
        {
            RaiseEvent((TBaseEvent)theEvent);
        }
    }
}