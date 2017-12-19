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
            public static class ChangePassword
            {
                internal class Domain : DomainCommand
                {
                    [UsedImplicitly] Domain() {}
                    public Domain(Guid accountId, string oldPassword, Password newPassword)
                    {
                        OldContract.Argument(() => accountId, () => newPassword).NotNullOrDefault();
                        OldContract.Argument(() => oldPassword).NotNullEmptyOrWhiteSpace();

                        AccountId = accountId;
                        OldPassword = oldPassword;
                        NewPassword = newPassword;
                    }

                    public Guid AccountId { get; private set; }
                    public string OldPassword { get; private set; }
                    public Password NewPassword { get; private set; }
                }

                public class UI : DomainCommand, IValidatableObject
                {
                    public UI(Guid accountId) => AccountId = accountId;

                    [Required] [EntityId] public Guid AccountId { get; private set; }
                    [Required] public string OldPassword { get; set; }
                    [Required] public string NewPassword { get; set; }

                    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Password.Validate(NewPassword, this, () => NewPassword);

                    internal Domain ToDomainCommand() => new Domain(AccountId, OldPassword, new Password(NewPassword) );
                }
            }
        }
    }
}