using System;

namespace Composable.CQRS.EventSourcing
{
    public interface IGetAggregateRootEntityEventEntityId<TEventInterface>
    {
        Guid GetId(TEventInterface @event);
    }
}