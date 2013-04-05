using System;
using System.Collections.Generic;
using Composable.DomainEvents;

namespace Composable.CQRS.Command
{
    public class CommandSuccessResponse : ICommandSuccessResponse
    {
        public Guid CommandId { get; set; }

        public IDomainEvent[] Events { get; set; }
    }
}
