using System;

namespace Composable.CQRS.Command
{
    public class CommandSuccess : Event
    {
        public Guid CommandId { get; set; }
    }
}
