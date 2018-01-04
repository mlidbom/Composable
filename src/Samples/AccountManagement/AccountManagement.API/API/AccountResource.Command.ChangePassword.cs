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
            public static class ChangePassword
            {
                [TypeId("F9074BCF-39B3-4C76-993A-9C27F3E71279")]internal class Domain : TransactionalExactlyOnceDeliveryCommand
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

                [TypeId("43922809-0404-4392-B754-38928C905EBF")]public class UI : TransactionalExactlyOnceDeliveryCommand, IValidatableObject
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