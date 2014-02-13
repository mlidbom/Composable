using System.Collections.Generic;
using Composable.System.Linq;

namespace Composable.CQRS.Command
{
    public class ValidationFailure : IValidationFailure
    {
        public string Message { get; set; }
    }
}