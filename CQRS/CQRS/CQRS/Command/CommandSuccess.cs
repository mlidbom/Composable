using System;

namespace Composable.CQRS.Command
{
    public class CommandSuccessResponse : ICommandResponseMessage
    {
        public Guid CommandId { get; set; }
    }
}
