using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Composable.Messaging.Buses
{
    public class CommandValidationFailureException : Exception
    {
        public IEnumerable<ValidationResult> Failures { get; }

        public CommandValidationFailureException(IEnumerable<ValidationResult> failures) : base(CreateMessage(failures)) => Failures = failures;

        static string CreateMessage(IEnumerable<ValidationResult> failures) => string.Join(Environment.NewLine, failures);
    }
}
