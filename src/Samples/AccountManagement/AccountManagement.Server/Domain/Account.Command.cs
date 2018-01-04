using System;
using AccountManagement.API;
using Composable;
using Composable.Contracts;
using Composable.Messaging.Commands;
using JetBrains.Annotations;

namespace AccountManagement.Domain
{
    partial class Account
    {
        internal static class Command
        {
            [TypeId("0EAC052B-3185-4AAA-9267-5073EEE15D5A")]internal class ChangeEmail : TransactionalExactlyOnceDeliveryCommand
            {
                [UsedImplicitly] ChangeEmail() {}
                public ChangeEmail(Guid accountId, Email email)
                {
                    OldContract.Argument(() => accountId, () => email).NotNullOrDefault();

                    AccountId = accountId;
                    Email = email;
                }
                public ChangeEmail(AccountResource.Command.ChangeEmail uiCommand):this(uiCommand.AccountId, Email.Parse(uiCommand.Email)){}

                public Guid AccountId { get; private set; }
                public Email Email { get; private set; }
            }

            [TypeId("F9074BCF-39B3-4C76-993A-9C27F3E71279")]internal class ChangePassword : TransactionalExactlyOnceDeliveryCommand
            {
                [UsedImplicitly] ChangePassword() {}
                public ChangePassword(Guid accountId, string oldPassword, Password newPassword)
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
        }
    }
}