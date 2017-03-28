using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.DDD;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Events;
using Composable.Persistence.EventSourcing;
using Composable.System;
using Composable.System.Linq;

namespace Composable.Persistence.EventStore.AggregateRoots
{
    public partial class AggregateRoot<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface> : VersionedPersistentEntity<TAggregateRoot>, IEventStored
        where TAggregateRoot : AggregateRoot<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
        where TAggregateRootBaseEventInterface : class, IAggregateRootEvent
        where TAggregateRootBaseEventClass : AggregateRootEvent, TAggregateRootBaseEventInterface
    {
        IUtcTimeTimeSource TimeSource { get; set; }

        [Obsolete("Only for infrastructure", true)]
        public AggregateRoot():this(DateTimeNowTimeSource.Instance){ }

        int _insertedVersionToAggregateVersionOffset = 0;

        //Yes empty. Id should be assigned by an action and it should be obvious that the aggregate in invalid until that happens
        protected AggregateRoot(IUtcTimeTimeSource timeSource) : base(Guid.Empty)
        {
            Contract.Assert.That(timeSource != null, "timeSource != null");
            Contract.Assert.That(typeof(TAggregateRootBaseEventInterface).IsInterface, "typeof(TAggregateRootBaseEventInterface).IsInterface");
            TimeSource = timeSource;
            _eventHandlersEventDispatcher.Register().IgnoreUnhandled<IAggregateRootEvent>();
        }

        readonly IList<IAggregateRootEvent> _unCommittedEvents = new List<IAggregateRootEvent>();
        readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateRootBaseEventInterface> _eventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateRootBaseEventInterface>();
        readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateRootBaseEventInterface> _eventHandlersEventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateRootBaseEventInterface>();

        protected void RaiseEvent(TAggregateRootBaseEventClass theEvent)
        {
            theEvent.AggregateRootVersion = Version + 1;
            theEvent.UtcTimeStamp = TimeSource.UtcNow;
            if (Version == 0)
            {
                if(!(theEvent is IAggregateRootCreatedEvent))
                {
                    throw new Exception($"The first raised event type {theEvent.GetType()} did not inherit {nameof(IAggregateRootCreatedEvent)}");
                }
                theEvent.AggregateRootVersion = 1;
            }else
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
            _eventHandlersEventDispatcher.Dispatch(theEvent);
        }

        protected IEventHandlerRegistrar<TAggregateRootBaseEventInterface> RegisterEventAppliers() => _eventDispatcher.RegisterHandlers();

        // ReSharper disable once UnusedMember.Global todo: coverage
        protected IEventHandlerRegistrar<TAggregateRootBaseEventInterface> RegisterEventHandlers() => _eventHandlersEventDispatcher.RegisterHandlers();

        void ApplyEvent(TAggregateRootBaseEventInterface theEvent)
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

        IEnumerable<IAggregateRootEvent> IEventStored.GetChanges() => _unCommittedEvents;

        void IEventStored.SetTimeSource(IUtcTimeTimeSource timeSource)
        {
            TimeSource = timeSource;
        }

        void IEventStored.LoadFromHistory(IEnumerable<IAggregateRootEvent> history)
        {
            history.ForEach(theEvent => ApplyEvent((TAggregateRootBaseEventInterface)theEvent));
            var maxInsertedVersion = history.Max(@event => ((AggregateRootEvent)@event).InsertedVersion);
            if(maxInsertedVersion != Version)
            {
                _insertedVersionToAggregateVersionOffset = maxInsertedVersion - Version;
            }
        }
    }
}