using System;

namespace Composable.CQRS.Command
{
    public class CommandDomainValidationExceptionResponse : ICommandFailedResponse
    {
        public Guid CommandId { get; set; }
        public string Message { get; set; }
        public string[] InvalidMembers { get; set; }
    }
}
