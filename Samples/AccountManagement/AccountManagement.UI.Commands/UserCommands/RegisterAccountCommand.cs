using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AccountManagement.UI.Commands.ValidationAttributes;
using Composable.DDD;
using Composable.System;
using Composable.System.ComponentModel.DataAnnotations;
using Composable.System.Linq;

namespace AccountManagement.UI.Commands.UserCommands
{
    public class RegisterAccountCommand : ValueObject<RegisterAccountCommand>, IValidatableObject
    {
        //Note the use of a custom validation attribute.
        [Required(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "IdInvalid")]
        [EntityId(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "IdMissing")]
        public Guid AccountId { get; set; }

        //Note the use of a custom validation attribute.
        [Email(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "EmailInvalid")]
        [Required(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "EmailMissing")]
        public string Email { get; set; }

        [Required(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "PasswordMissing")]
        public string Password { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return ValidatePassword();
        }

        IEnumerable<ValidationResult> ValidatePassword()
        {
            var policyFailures = Domain.Shared.Password.Policy.GetPolicyFailures(Password).ToList();
            if(policyFailures.Any())
            {
                switch(policyFailures.First())
                {
                    case Domain.Shared.Password.Policy.Failures.BorderedByWhitespace:
                        yield return this.CreateValidationResult(RegisterAccountCommandResources.Password_BorderedByWhitespace, () => Password);
                        break;
                    case Domain.Shared.Password.Policy.Failures.MissingLowerCaseCharacter:
                        yield return this.CreateValidationResult(RegisterAccountCommandResources.Password_MissingLowerCaseCharacter, () => Password);
                        break;
                    case Domain.Shared.Password.Policy.Failures.MissingUppercaseCharacter:
                        yield return this.CreateValidationResult(RegisterAccountCommandResources.Password_MissingUpperCaseCharacter, () => Password);
                        break;
                    case Domain.Shared.Password.Policy.Failures.ShorterThanFourCharacters:
                        yield return this.CreateValidationResult(RegisterAccountCommandResources.Password_ShorterThanFourCharacters, () => Password);
                        break;
                    default:
                        throw new Exception($"Unknown password failure type {policyFailures.First()}");
                }
            }
        }
    }
}
