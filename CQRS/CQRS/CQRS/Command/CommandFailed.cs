using System;

namespace Composable.CQRS.Command
{
    public abstract class CommandFailed : Event
    {
        [Obsolete("This is only for serialization", true)]
        public CommandFailed()
        {}

        public CommandFailed( Guid commandId, string message)
        {
            
        }

        public Guid CommandId { get; set; }
        public string Message { get; set; }
    }
}
