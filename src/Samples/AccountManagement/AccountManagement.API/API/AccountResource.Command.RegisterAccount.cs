using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AccountManagement.API.ValidationAttributes;
using AccountManagement.Domain;
using Composable.Contracts;
using Composable.Messaging.Commands;
using JetBrains.Annotations;

namespace AccountManagement.API
{
    public partial class AccountResource
    {
        public static partial class Command
        {
            public static class Register
            {
                public class DomainCommand : DomainCommand<AccountResource>
                {
                    [UsedImplicitly] DomainCommand() { }
                    public DomainCommand(Guid accountId, Password password, Email email)
                    {
                        OldContract.Argument(() => accountId, () => password, () => email).NotNullOrDefault();
                        AccountId = accountId;
                        Password = password;
                        Email = email;
                    }

                    public Guid AccountId { get; private set; }
                    public Password Password { get; private set; }
                    public Email Email { get; private set; }
                }

                public class UICommand : DomainCommand<AccountResource>, IValidatableObject
                {
                    public UICommand() {}
                    public UICommand(Guid accountId, string email, string password)
                    {
                        AccountId = accountId;
                        Email = email;
                        Password = password;
                    }

                    //Note the use of a custom validation attributes.
                    [EntityId(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "IdInvalid")]
                    [Required(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "IdMissing")] public Guid AccountId { [UsedImplicitly] get; set; } = Guid.NewGuid();

                    [Email(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "EmailInvalid")]
                    [Required(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "EmailMissing")] public string Email { [UsedImplicitly] get; set; }

                    [Required(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "PasswordMissing")] public string Password { get; set; }

                    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Domain.Password.Validate(Password, this, () => Password);

                    public DomainCommand ToDomainCommand() => new DomainCommand(AccountId, new Password(Password), Domain.Email.Parse(Email));
                }
            }
        }
    }
}
