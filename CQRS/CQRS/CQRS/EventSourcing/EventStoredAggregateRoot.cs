using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DDD;
using Composable.DomainEvents;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing
{
    public class EventStoredAggregateRoot<TEntity> : VersionedPersistentEntity<TEntity>, IEventStored
        where TEntity : EventStoredAggregateRoot<TEntity>
    {
        private readonly IList<IAggregateRootEvent> _unCommittedEvents = new List<IAggregateRootEvent>();

        //Yes empty. Id should be assigned by action and it should be obvious that the aggregate in invalid until that happens
        protected EventStoredAggregateRoot() : base(Guid.Empty) { }

        private readonly Dictionary<Type, Action<IAggregateRootEvent>> _registeredEvents = new Dictionary<Type, Action<IAggregateRootEvent>>();
        private void RegisterEventHandler<TEvent>(Action<TEvent> eventHandler) where TEvent : class, IAggregateRootEvent
        {
            _registeredEvents.Add(typeof(TEvent), theEvent => eventHandler(theEvent as TEvent));
        }
        private void RegisterEventHandler(Type eventType, Action<IAggregateRootEvent> eventHandler)
        {
            _registeredEvents.Add(eventType, eventHandler);
        }

        protected void Register(params HandlerRegistration[] handlerRegistrations)
        {
            foreach (var handlerRegistration in handlerRegistrations)
            {
                RegisterEventHandler(handlerRegistration.EventType, handlerRegistration.Handler);
            }
        }

        protected void ApplyEvent(IAggregateRootEvent evt)
        {
            DoApply(evt);
            evt.AggregateRootVersion = ++Version;
            evt.AggregateRootId = Id;
            _unCommittedEvents.Add(evt);
            DomainEvent.Raise(evt);//Fixme: Don't do this synchronously!
        }

        private void DoApply(IAggregateRootEvent evt)
        {
            Action<IAggregateRootEvent> handler;

            if (_registeredEvents.TryGetValue(evt.GetType(), out handler))
            {
                handler(evt);
            }
            else
            {
                throw new RegisteredHandlerMissingException(this.GetType(), evt);
            }
        }

        void IEventStored.LoadFromHistory(IEnumerable<IAggregateRootEvent> evts)
        {
            evts.ForEach(DoApply);
            Version = evts.Max(e => e.AggregateRootVersion);
        }

        void IEventStored.AcceptChanges()
        {
            _unCommittedEvents.Clear();
        }

        IEnumerable<IAggregateRootEvent> IEventStored.GetChanges()
        {
            return _unCommittedEvents;
        }
    }
}
