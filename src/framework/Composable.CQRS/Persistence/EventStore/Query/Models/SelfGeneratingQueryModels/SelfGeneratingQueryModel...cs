using System;
using System.Collections.Generic;
using Composable.Contracts;
using Composable.DDD;
using Composable.Messaging.Events;
using Composable.System.Linq;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public partial class SelfGeneratingQueryModel<TAggregateRoot, TAggregateRootBaseEventInterface> : VersionedPersistentEntity<TAggregateRoot>
        where TAggregateRoot : SelfGeneratingQueryModel<TAggregateRoot, TAggregateRootBaseEventInterface>
        where TAggregateRootBaseEventInterface : class, IAggregateRootEvent
    {
        //Yes empty. Id should be assigned by an action and it should be obvious that the aggregate in invalid until that happens
        protected SelfGeneratingQueryModel() : base(Guid.Empty)
        {
            OldContract.Assert.That(typeof(TAggregateRootBaseEventInterface).IsInterface, "typeof(TAggregateRootBaseEventInterface).IsInterface");
        }

        readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateRootBaseEventInterface> _eventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateRootBaseEventInterface>();

        protected IEventHandlerRegistrar<TAggregateRootBaseEventInterface> RegisterEventAppliers() => _eventDispatcher.RegisterHandlers();

        public void ApplyEvent(TAggregateRootBaseEventInterface theEvent)
        {
            if(theEvent is IAggregateRootCreatedEvent)
            {
                SetIdBeVerySureYouKnowWhatYouAreDoing(theEvent.AggregateRootId);
            }

            Version = theEvent.AggregateRootVersion;
            _eventDispatcher.Dispatch(theEvent);
        }

        public void LoadFromHistory(IEnumerable<IAggregateRootEvent> history)
        {
            Contract.State.Assert(Version == 0);
            history.ForEach(theEvent => ApplyEvent((TAggregateRootBaseEventInterface)theEvent));
        }
    }
}