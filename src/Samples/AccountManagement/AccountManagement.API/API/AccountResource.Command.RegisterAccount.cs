using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AccountManagement.API.ValidationAttributes;
using Composable;
using Composable.Messaging.Commands;
using JetBrains.Annotations;

namespace AccountManagement.API
{
    public partial class AccountResource
    {
        public static partial class Command
        {
            [TypeId("B1406B7E-A51C-4487-845C-AB7326465AD0")]
            public class Register : TransactionalExactlyOnceDeliveryCommand<Register.RegistrationAttemptResult>, IValidatableObject
            {
                public Register() {}
                public Register(Guid accountId, string email, string password)
                {
                    AccountId = accountId;
                    Email = email;
                    Password = password;
                }

                //Note the use of a custom validation attributes.
                [EntityId(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "IdInvalid")]
                [Required(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "IdMissing")]
                public Guid AccountId { [UsedImplicitly] get; set; } = Guid.NewGuid();

                [Email(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "EmailInvalid")]
                [Required(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "EmailMissing")]
                public string Email { [UsedImplicitly] get; set; }

                [Required(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "PasswordMissing")]
                public string Password { get; set; }

                public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Domain.Password.Validate(Password, this, () => Password);

                public enum RegistrationAttemptResult
                {
                    Successful = 1,
                    EmailAlreadyRegistered = 2
                }
            }
        }
    }
}
