using System;
using System.Collections.Generic;
using Composable.Contracts;
using Composable.DDD;
using Composable.Messaging.Events;
using Composable.System.Linq;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public partial class SelfGeneratingQueryModel<TAggregate, TAggregateEvent> : VersionedPersistentEntity<TAggregate>
        where TAggregate : SelfGeneratingQueryModel<TAggregate, TAggregateEvent>
        where TAggregateEvent : class, IAggregateEvent
    {
        //Yes empty. Id should be assigned by an action and it should be obvious that the aggregate in invalid until that happens
        protected SelfGeneratingQueryModel() : base(Guid.Empty)
        {
            OldContract.Assert.That(typeof(TAggregateEvent).IsInterface, "typeof(TAggregateEvent).IsInterface");
        }

        readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateEvent> _eventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateEvent>();

        protected IEventHandlerRegistrar<TAggregateEvent> RegisterEventAppliers() => _eventDispatcher.RegisterHandlers();

        public void ApplyEvent(TAggregateEvent theEvent)
        {
            if(theEvent is IAggregateCreatedEvent)
            {
                SetIdBeVerySureYouKnowWhatYouAreDoing(theEvent.AggregateRootId);
            }

            Version = theEvent.AggregateRootVersion;
            _eventDispatcher.Dispatch(theEvent);
        }

        public void LoadFromHistory(IEnumerable<IAggregateEvent> history)
        {
            Contract.State.Assert(Version == 0);
            history.ForEach(theEvent => ApplyEvent((TAggregateEvent)theEvent));
        }
    }
}