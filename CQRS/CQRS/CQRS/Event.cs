using System;
using NServiceBus;

namespace Composable.CQRS
{
    public class Event : IEvent
    {
        public Event()
        {
            Id = Guid.NewGuid();
        }
        public Guid Id { get; set; }
    }
}
