using System;
using Composable.DDD;
using Composable.DomainEvents;

namespace Composable.CQRS.Command
{
    //todo:probably should not be IDomainEvent. Should it be here?
    public class CommandSucceededEvent : ValueObject<CommandSucceededEvent>, IDomainEvent
    {
        public Guid CommandId { get; set; }
    }
}