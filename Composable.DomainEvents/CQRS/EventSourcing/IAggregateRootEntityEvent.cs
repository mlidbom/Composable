using System;

namespace Composable.CQRS.EventSourcing
{
    public interface IAggregateRootEntityEvent
    {
        Guid EntityId { get; }
    }
}