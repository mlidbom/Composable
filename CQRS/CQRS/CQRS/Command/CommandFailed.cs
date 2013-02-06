using System;

namespace Composable.CQRS.Command
{
    public class CommandFailed : Event
    {
        public Guid CommandId { get; set; }
        public string Message { get; set; }
    }
}
