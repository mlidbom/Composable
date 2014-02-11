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
            ApplyAs(evt, evt.GetType());
            evt.AggregateRootVersion = ++Version;
            evt.AggregateRootId = Id;
            _unCommittedEvents.Add(evt);
            DomainEvent.Raise(evt);//Fixme: Don't do this synchronously!
        }

        /// <summary>
        /// Dispatches an event to handlers as if it was the type specified.
        /// It is intended to let inheriting classes use the base class code to handle an event while 
        /// still mainining the ability to easily modify the logic and choose when to run the base class. (Usually before or after the subclass code..) 
        /// </summary>
        protected void ApplyAs<TApplyAs>(IAggregateRootEvent evt)
        {
            ApplyAs(evt, typeof(TApplyAs));
        }
        
        private void ApplyAs(IAggregateRootEvent evt, Type applyAs)
        {
            Action<IAggregateRootEvent> handler;

            if (_registeredEvents.TryGetValue(applyAs, out handler))
            {
                handler(evt);
            }
            else
            {
                throw new RegisteredHandlerMissingException(this.GetType(), evt, applyAs);
            }
        }

        void IEventStored.LoadFromHistory(IEnumerable<IAggregateRootEvent> history)
        {
            history.ForEach(evt => ApplyAs(evt, evt.GetType()));
            Version = history.Max(e => e.AggregateRootVersion);
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
