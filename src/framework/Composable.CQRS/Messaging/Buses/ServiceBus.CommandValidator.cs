using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Composable.Messaging.Buses
{
    static class CommandValidator
    {
        public static void AssertCommandIsValid(BusApi.ICommand command)
        {
            var failures = ValidationFailures(command);
            if(failures.Any())
            {
                throw new CommandValidationFailureException(failures);
            }
        }

        static IReadOnlyList<ValidationResult> ValidationFailures(object command)
        {
            var context = new ValidationContext(command, serviceProvider: null, items: null);
            var results = new List<ValidationResult>();

            Validator.TryValidateObject(command, context, results, validateAllProperties: true);
            return results;
        }
    }
}
