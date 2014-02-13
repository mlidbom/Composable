using System;
using System.Collections.Generic;
using Composable.DDD;
using Composable.DomainEvents;

namespace Composable.CQRS.Command
{
    public class CommandFailedEvent : ValueObject<CommandFailedEvent>, IDomainEvent
    {
        public Guid CommandId { get; set; }
        public object Failures { get; set; }
        public IEnumerable<string> Messages { get; set; }
    }
}