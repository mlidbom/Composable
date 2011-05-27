using System;
using System.Collections.Generic;
using Composable.DDD;
using Composable.DomainEvents;
using Composable.System.Linq;
using System.Linq;
using Newtonsoft.Json;

namespace Composable.CQRS.EventSourcing
{
    public class AggregateRoot<TEntity> : PersistentEntity<TEntity>, IEventStored where TEntity : PersistentEntity<TEntity>
    {
        private readonly IList<IAggregateRootEvent> _unCommittedEvents = new List<IAggregateRootEvent>();
        
        protected AggregateRoot() { }

        public AggregateRoot(Guid id) : base(id) { }

        private readonly Dictionary<Type, Action<IAggregateRootEvent>> _registeredEvents = new Dictionary<Type, Action<IAggregateRootEvent>>();
        protected void RegisterEventHandler<TEvent>(Action<TEvent> eventHandler) where TEvent : class, IDomainEvent
        {
            _registeredEvents.Add(typeof(TEvent), theEvent => eventHandler(theEvent as TEvent));
        }

        /// <param name="_this">Only used for our generic constraints to catch problems when trying to apply an unsupported event type. The actual instance is never used.</param>
        protected void ApplyEvent<TThis, TEvent>(TThis _this, TEvent evt) where TThis : IEventApplier<TEvent> where TEvent : IAggregateRootEvent
        {
            ApplyEvent(evt);
        }

        protected void ApplyEvent(IAggregateRootEvent evt)
        {
            DoApply(evt);
            evt.AggregateRootVersion = ++Version;
            evt.AggregateRootId = Id;
            _unCommittedEvents.Add(evt);
            //DomainEvent.Raise(evt);//Fixme: Don't do this synchronously!
        }

        public int Version { get; set; }

        private void DoApply(IAggregateRootEvent evt)
        {
            Action<IAggregateRootEvent> handler;

            if (_registeredEvents.TryGetValue(evt.GetType(), out handler))
            {
                handler(evt);
            }
            else
            {
                dynamic me = this;
                me.Apply((dynamic)evt);
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

    public interface IEventStored
    {
        Guid Id { get; }
        int Version { get; }
        IEnumerable<IAggregateRootEvent> GetChanges();
        void AcceptChanges();
        void LoadFromHistory(IEnumerable<IAggregateRootEvent> evts);
    }
}