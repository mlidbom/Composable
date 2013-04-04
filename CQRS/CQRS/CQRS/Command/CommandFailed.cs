using System;

namespace Composable.CQRS.Command
{
    public abstract class CommandFailed : Event
    {
        public Guid CommandId { get; set; }
        public string Message { get; set; }
        public string[] InvalidMembers { get; set; }
    }
}
