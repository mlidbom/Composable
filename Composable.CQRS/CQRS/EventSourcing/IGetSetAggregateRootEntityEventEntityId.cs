using System;

namespace Composable.CQRS.EventSourcing
{
    public interface IGetSetAggregateRootEntityEventEntityId<TEventClass, TEventInterface> : IGetAggregateRootEntityEventEntityId<TEventInterface>
    {
        void SetEntityId(TEventClass @event, Guid id);
    }
}