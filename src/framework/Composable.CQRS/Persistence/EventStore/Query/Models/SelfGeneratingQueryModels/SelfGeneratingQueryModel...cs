using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.DDD;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Events;
using Composable.System.Linq;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public partial class SelfGeneratingQueryModel<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface> : VersionedPersistentEntity<TAggregateRoot>
        where TAggregateRoot : SelfGeneratingQueryModel<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
        where TAggregateRootBaseEventInterface : class, IAggregateRootEvent
        where TAggregateRootBaseEventClass : AggregateRootEvent, TAggregateRootBaseEventInterface
    {
        IUtcTimeTimeSource TimeSource { get; set; }

        [Obsolete("Only for infrastructure", true)] protected SelfGeneratingQueryModel():this(DateTimeNowTimeSource.Instance){ }

        int _insertedVersionToAggregateVersionOffset = 0;

        //Yes empty. Id should be assigned by an action and it should be obvious that the aggregate in invalid until that happens
        protected SelfGeneratingQueryModel(IUtcTimeTimeSource timeSource) : base(Guid.Empty)
        {
            OldContract.Assert.That(timeSource != null, "timeSource != null");
            OldContract.Assert.That(typeof(TAggregateRootBaseEventInterface).IsInterface, "typeof(TAggregateRootBaseEventInterface).IsInterface");
            TimeSource = timeSource;
            _eventHandlersEventDispatcher.Register().IgnoreUnhandled<TAggregateRootBaseEventInterface>();
        }

        readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateRootBaseEventInterface> _eventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateRootBaseEventInterface>();
        readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateRootBaseEventInterface> _eventHandlersEventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateRootBaseEventInterface>();

        int _raiseEventReentrancyLevel = 0;
        bool _applyingEvents;

        protected IEventHandlerRegistrar<TAggregateRootBaseEventInterface> RegisterEventAppliers() => _eventDispatcher.RegisterHandlers();

        // ReSharper disable once UnusedMember.Global todo: coverage
        protected IEventHandlerRegistrar<TAggregateRootBaseEventInterface> RegisterEventHandlers() => _eventHandlersEventDispatcher.RegisterHandlers();

        void ApplyEvent(TAggregateRootBaseEventInterface theEvent)
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

        public void LoadFromHistory(IEnumerable<IAggregateRootEvent> history)
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