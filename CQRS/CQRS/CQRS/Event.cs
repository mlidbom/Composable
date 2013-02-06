using System;
using NServiceBus;

namespace Composable.CQRS
{
    public abstract class Event : IEvent
    {
        protected Event()
        {
            EventId = Guid.NewGuid();
        }
        
        public Guid EventId { get; set; }
    }
}
