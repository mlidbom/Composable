using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Composable.DDD;
using Composable.System.ComponentModel.DataAnnotations;

namespace AccountManagement.UI.Commands.UserCommands
{
    public class RegisterAccountCommand : ValueObject<RegisterAccountCommand>, IValidatableObject
    {
        [Required]
        public string Email { get; set; }


        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if(!Domain.Shared.Email.IsValidEmail(Email))
            {
                yield return this.CreateValidationResult(RegisterAccountCommandResources.InvalidEmail, () => Email);
            }
        }
    }
}