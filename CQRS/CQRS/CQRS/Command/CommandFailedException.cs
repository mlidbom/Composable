using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Composable.CQRS.Command
{
    [Obsolete("Please use DomainCommandValidationException instead. This exception is badly named.")]
    public class CommandFailedException : DomainCommandValidationException
    {
        public CommandFailedException(string message) : base(message) {}

        public CommandFailedException(string message, IEnumerable<string> invalidMembers) : base(message, invalidMembers) {}

        public CommandFailedException(string message, params string[] invalidMembers) : base(message, invalidMembers) {}

        public CommandFailedException(string message, IEnumerable<Expression<Func<object>>> memberAccessors) : base(message, memberAccessors) {}

        public CommandFailedException(string message, params Expression<Func<object>>[] memberAccessors) : base(message, memberAccessors) {}
    }
}
