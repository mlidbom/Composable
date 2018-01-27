using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AccountManagement.API.ValidationAttributes;
using Composable.Messaging;
using JetBrains.Annotations;
// ReSharper disable MemberCanBeMadeStatic.Global Because we want these members to be accessed through the fluent API we don't want to make them static.

namespace AccountManagement.API
{
    public partial class AccountResource
    {
        public static partial class Command
        {
            public partial class Register : ExactlyOnceCommand<Register.RegistrationAttemptResult>, IValidatableObject
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

                public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Domain.Passwords.Password.Validate(Password, this, () => Password);

                internal MessagingApi.Remote.ExactlyOnce.IExactlyOnceCommand<RegistrationAttemptResult> WithValues(Guid accountId, string email, string password) => new Register(accountId, email, password);
            }
        }
    }
}
