using System;
using Newtonsoft.Json;

namespace Composable.CQRS.EventSourcing
{
    public class AggregateRootEvent : IAggregateRootEvent
    {
        protected AggregateRootEvent()
        {
            ((IAggregateRootEvent)this).Id = Guid.NewGuid();
        }

        [JsonProperty] Guid IAggregateRootEvent.Id { get; set; }
        int IAggregateRootEvent.AggregateRootVersion { get; set; }
        Guid IAggregateRootEvent.AggregateRootId { get; set; }
    }
}