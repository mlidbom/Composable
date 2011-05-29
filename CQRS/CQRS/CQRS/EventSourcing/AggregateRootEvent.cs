using System;
using Newtonsoft.Json;

namespace Composable.CQRS.EventSourcing
{
    public class AggregateRootEvent : IAggregateRootEvent
    {
        protected AggregateRootEvent()
        {
            ((IAggregateRootEvent)this).EventId = Guid.NewGuid();
        }

        Guid IAggregateRootEvent.EventId { get; set; }
        int IAggregateRootEvent.AggregateRootVersion { get; set; }
        Guid IAggregateRootEvent.AggregateRootId { get; set; }
    }
}