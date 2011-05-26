using System;
using System.Collections.Generic;
using Composable.DDD;
using Composable.DomainEvents;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing
{
    public class EventStored<TEntity> : PersistentEntity<TEntity>, IEventStored where TEntity : PersistentEntity<TEntity>
    {
        private readonly IList<IAggregateRootEvent> _appliedEvents = new List<IAggregateRootEvent>();
        
        protected EventStored() { }

        public EventStored(Guid id) : base(id) { }

        private readonly Dictionary<Type, Action<IDomainEvent>> _registeredEvents = new Dictionary<Type, Action<IDomainEvent>>();
        protected void RegisterEventHandler<TEvent>(Action<TEvent> eventHandler) where TEvent : class, IDomainEvent
        {
            _registeredEvents.Add(typeof(TEvent), theEvent => eventHandler(theEvent as TEvent));
        }

        protected void ApplyEvent(IAggregateRootEvent evt)
        {
            evt.Version = Version++;
            evt.EntityId = Id;
            DoApply(evt);
        }

        public int Version { get; set; }

        private void DoApply(IAggregateRootEvent evt)
        {
            Action<IDomainEvent> handler;

            if (!_registeredEvents.TryGetValue(evt.GetType(), out handler))
                throw new Exception(string.Format("The requested domain event '{0}' is not registered in '{1}'", evt.GetType().FullName, GetType().FullName));

            handler(evt);

            _appliedEvents.Add(evt);
        }

        public virtual void LoadStateFromEvents(IEnumerable<IAggregateRootEvent> evts)
        {
            evts.ForEach(DoApply);
        }

        public IEnumerable<IAggregateRootEvent> GetChanges()
        {
            return _appliedEvents;
        }
    }

    public interface IEventStored
    {
        IEnumerable<IAggregateRootEvent> GetChanges();
    }
}