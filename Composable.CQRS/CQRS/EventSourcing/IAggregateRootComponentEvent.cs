using System;

namespace Composable.CQRS.EventSourcing
{
    public interface IAggregateRootComponentEvent
    {
        Guid ComponentId { get; }
    }
}