using System;

namespace Composable.CQRS.Command
{
    public abstract class CommandSuccess : Event
    {
        public Guid CommandId { get; set; }
        public string Message { get; set; }
    }
}
