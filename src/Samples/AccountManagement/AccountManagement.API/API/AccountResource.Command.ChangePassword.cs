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
                public class Domain : DomainCommand
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

                    public Guid AccountId { get; set; }
                    public string OldPassword { get; set; }
                    public Password NewPassword { get; set; }
                }

                public class UI : IValidatableObject
                {
                    [Required] [EntityId] public Guid AccountId { get; set; }
                    [Required] public string OldPassword { get; set; }
                    [Required] public string NewPassword { get; set; }

                    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => AccountManagement.Domain.Password.Validate(NewPassword, this, () => NewPassword);

                    public Domain ToDomainCommand() => new Domain(AccountId, OldPassword, new Password(NewPassword) );
                }
            }
        }
    }
}