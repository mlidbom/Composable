using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AccountManagement.API.ValidationAttributes;
using Composable.Messaging.Commands;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace AccountManagement.API
{
    public partial class AccountResource
    {
        public AccountResourceCommands Commands { get; private set; }
        public class AccountResourceCommands
        {
            [JsonProperty] Guid _accountId;

            [UsedImplicitly] AccountResourceCommands() {}

            public AccountResourceCommands(AccountResource accountResource) => _accountId = accountResource.Id;

            public ChangeEmailCommand ChangeEmail(string email) => new ChangeEmailCommand()
                                                                   {
                                                                       Email = email,
                                                                       AccountId = _accountId
                                                                   };

            public ChangePasswordCommand ChangePassword(string oldPassword, string newPassword) => new ChangePasswordCommand()
                                                                                                   {
                                                                                                       AccountId = _accountId,
                                                                                                       OldPassword = oldPassword,
                                                                                                       NewPassword = newPassword
                                                                                                   };
        }

        public class ChangePasswordCommand : DomainCommand, IValidatableObject
        {
            [Required] [EntityId] public Guid AccountId { get; set; }
            [Required] public string OldPassword { get; set; }
            [Required] public string NewPassword { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Domain.Password.Validate(NewPassword, this, () => NewPassword);
        }

        public class ChangeEmailCommand : DomainCommand
        {
            [Required] [EntityId] public Guid AccountId { get; set; }
            [Required] [Email] public string Email { get; set; }
        }

        public class RegisterAccountCommand : DomainCommand<AccountResource>, IValidatableObject
        {
            public RegisterAccountCommand() { }
            public RegisterAccountCommand(Guid accountId, string email, string password)
            {
                AccountId = accountId;
                Email = email;
                Password = password;
            }

            //Note the use of a custom validation attribute.
            [Required(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "IdInvalid")]
            [EntityId(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "IdMissing")]
            public Guid AccountId { [UsedImplicitly] get; set; } = Guid.NewGuid();

            //Note the use of a custom validation attribute.
            [Email(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "EmailInvalid")]
            [Required(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "EmailMissing")]
            public string Email { [UsedImplicitly] get; set; }

            [Required(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "PasswordMissing")]
            // ReSharper disable once MemberCanBePrivate.Global
            public string Password { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Domain.Password.Validate(Password, this, () => Password);

        }
    }
}
