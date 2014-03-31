using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AccountManagement.UI.Commands.Tests.UserCommands
{
    public class CommandValidator {
        public static IEnumerable<ValidationResult> Validate(object command)
        {
            var context = new ValidationContext(command, serviceProvider: null, items: null);
            var results = new List<ValidationResult>();

            Validator.TryValidateObject(command, context, results, validateAllProperties: true);
            return results;
        }
    }
}