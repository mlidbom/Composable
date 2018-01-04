using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AccountManagement.API.ValidationAttributes;
using AccountManagement.Domain;
using Composable;
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
                [TypeId("B0CAD429-295D-43E7-8441-566B7887C7F0")]internal class TransactionalExactlyOnceDeliveryCommand : TransactionalExactlyOnceDeliveryCommand<RegistrationAttemptResult>
                {
                    [UsedImplicitly] TransactionalExactlyOnceDeliveryCommand() { }
                    public TransactionalExactlyOnceDeliveryCommand(Guid accountId, Password password, Email email)
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

                [TypeId("B1406B7E-A51C-4487-845C-AB7326465AD0")]public class UICommand : TransactionalExactlyOnceDeliveryCommand<RegistrationAttemptResult>, IValidatableObject
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

                    internal TransactionalExactlyOnceDeliveryCommand ToDomainCommand() => new TransactionalExactlyOnceDeliveryCommand(AccountId, new Password(Password), Domain.Email.Parse(Email));
                }

                public enum RegistrationAttemptResult
                {
                    Successful = 1,
                    EmailAlreadyRegistered=2
                }
            }
        }
    }
}
