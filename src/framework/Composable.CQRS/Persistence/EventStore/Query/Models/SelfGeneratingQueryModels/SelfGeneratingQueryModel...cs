using System;
using System.Collections.Generic;
using Composable.Contracts;
using Composable.DDD;
using Composable.Messaging.Events;
using Composable.System.Linq;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public partial class SelfGeneratingQueryModel<TAggregate, TAggregateBaseEventInterface> : VersionedPersistentEntity<TAggregate>
        where TAggregate : SelfGeneratingQueryModel<TAggregate, TAggregateBaseEventInterface>
        where TAggregateBaseEventInterface : class, IAggregateRootEvent
    {
        //Yes empty. Id should be assigned by an action and it should be obvious that the aggregate in invalid until that happens
        protected SelfGeneratingQueryModel() : base(Guid.Empty)
        {
            OldContract.Assert.That(typeof(TAggregateBaseEventInterface).IsInterface, "typeof(TAggregateBaseEventInterface).IsInterface");
        }

        readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateBaseEventInterface> _eventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateBaseEventInterface>();

        protected IEventHandlerRegistrar<TAggregateBaseEventInterface> RegisterEventAppliers() => _eventDispatcher.RegisterHandlers();

        public void ApplyEvent(TAggregateBaseEventInterface theEvent)
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
            history.ForEach(theEvent => ApplyEvent((TAggregateBaseEventInterface)theEvent));
        }
    }
}