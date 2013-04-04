using System;

namespace Composable.CQRS.Command
{
    public abstract class CommandSuccess : ICommandMessage
    {
        public Guid CommandId { get; set; }
        public string Message { get; set; }
    }
}
