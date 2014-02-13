using System;

namespace Composable.CQRS.Command
{
    public class CommandExecutionExceptionResponse : ICommandFailedResponse
    {
        public Guid CommandId { get; set; }
    }
}