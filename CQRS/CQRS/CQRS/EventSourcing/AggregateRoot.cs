using System;
using System.Collections.Generic;
using Composable.DDD;
using Composable.DomainEvents;
using Composable.System.Linq;
using System.Linq;
using Newtonsoft.Json;

namespace Composable.CQRS.EventSourcing
{
    public interface ISharedOwnershipAggregateRoot
    {
        void IntegrateExternallyRaisedEvent(IAggregateRootEvent evt);
    }

    public class AggregateRoot<TEntity> : VersionedPersistentEntity<TEntity>, IEventStored, ISharedOwnershipAggregateRoot where TEntity : AggregateRoot<TEntity>
    {
        private readonly IList<IAggregateRootEvent> _unCommittedEvents = new List<IAggregateRootEvent>();
        
        //Yes empty. Id should be assigned by action and it should be obvious that the aggregate in invalid until that happens
        protected AggregateRoot():base(Guid.Empty) { }

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

        void ISharedOwnershipAggregateRoot.IntegrateExternallyRaisedEvent(IAggregateRootEvent evt)
        {
            ApplyEvent(evt);
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