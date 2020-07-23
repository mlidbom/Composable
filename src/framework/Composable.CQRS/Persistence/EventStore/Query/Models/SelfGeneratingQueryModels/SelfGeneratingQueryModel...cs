using System;
using System.Collections.Generic;
using Composable.Contracts;
using Composable.DDD;
using Composable.Messaging.Events;
using Composable.SystemCE.LinqCE;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public partial class SelfGeneratingQueryModel<TQueryModel, TAggregateEvent> : VersionedEntity<TQueryModel>
        where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TAggregateEvent>
        where TAggregateEvent : class, IAggregateEvent
    {
        //Yes empty. Id should be assigned by an action and it should be obvious that the aggregate in invalid until that happens
        protected SelfGeneratingQueryModel() : base(Guid.Empty)
        {
            Contract.Assert.That(typeof(TAggregateEvent).IsInterface, "typeof(TAggregateEvent).IsInterface");
        }

        readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateEvent> _eventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateEvent>();

        protected IEventHandlerRegistrar<TAggregateEvent> RegisterEventAppliers() => _eventDispatcher.RegisterHandlers();

        public void ApplyEvent(TAggregateEvent theEvent)
        {
            if(theEvent is IAggregateCreatedEvent)
            {
#pragma warning disable 618 //Review OK: This is precisely the type of internal code this is supposed to use this "obsolete" method.
                SetIdBeVerySureYouKnowWhatYouAreDoing(theEvent.AggregateId);
#pragma warning restore 618
            }

            Version = theEvent.AggregateVersion;
            _eventDispatcher.Dispatch(theEvent);
        }

        public bool HandlesEvent(TAggregateEvent @event) => _eventDispatcher.Handles(@event);
        public bool HandlesEvent<THandled>() => _eventDispatcher.HandlesEvent<THandled>();

        public void LoadFromHistory(IEnumerable<IAggregateEvent> history)
        {
            Assert.State.Assert(Version == 0);
            history.ForEach(theEvent => ApplyEvent((TAggregateEvent)theEvent));
            AssertInvariantsAreMet();
        }

        protected virtual void AssertInvariantsAreMet(){}
    }
}