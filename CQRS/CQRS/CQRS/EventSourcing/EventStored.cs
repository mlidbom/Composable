using System;
using System.Collections.Generic;
using Composable.CQRS.EventSourcing;
using Composable.DDD;
using Composable.DomainEvents;
using Composable.System.Linq;

namespace Manpower.System.Web.Mvc.StuffThatDoesNotBelongHere
{
    public class EventStored<TEntity> : VersionedPersistentEntity<TEntity> where TEntity : VersionedPersistentEntity<TEntity>, new()
    {
        protected EventStored() { }

        public EventStored(Guid id) : base(id) { }

        /// <param name="_this">Only used for our generic constraints to catch problems when trying to apply an unsupported event type. The actual instance is never used.</param>
        protected void ApplyEvent<TThis, TEvent>(TThis _this, TEvent evt) where TThis : IEventApplier<TEvent> where TEvent : IDomainEvent
        {
            _this.Apply(evt);
            DomainEvent.Raise(evt);
        }

        private void DoApply(dynamic evt)
        {
            dynamic me = this;
            me.Apply(evt);
        }

        public virtual void LoadStateFromEvents(IEnumerable<IDomainEvent> evts)
        {
            evts.ForEach(DoApply);
        }
    }
}