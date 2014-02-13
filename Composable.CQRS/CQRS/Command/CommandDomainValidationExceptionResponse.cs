using System;
using System.Collections.Generic;

namespace Composable.CQRS.Command
{
    public class CommandDomainValidationExceptionResponse : ICommandFailedResponse
    {
        public Guid CommandId { get; set; }
        public string Message { get; set; }
        public List<string> InvalidMembers { get; set; }
    }
}
