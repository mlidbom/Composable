using System;
using AccountManagement.API;
using Composable.Contracts;
using Composable.Messaging.Commands;
using JetBrains.Annotations;
// ReSharper disable MemberHidesStaticFromOuterClass

namespace AccountManagement.Domain
{
    partial class Account
    {
        internal static class Command
        {
            internal class Register : TransactionalExactlyOnceDeliveryCommand<AccountResource.Commands.Register.RegistrationAttemptResult>
            {
                [UsedImplicitly] Register() { }
                internal Register(Guid accountId, Password password, Email email)
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

            internal class ChangeEmail : TransactionalExactlyOnceDeliveryCommand
            {
                [UsedImplicitly] ChangeEmail() {}
                internal ChangeEmail(Guid accountId, Email email)
                {
                    OldContract.Argument(() => accountId, () => email).NotNullOrDefault();

                    AccountId = accountId;
                    Email = email;
                }
                public ChangeEmail(AccountResource.Commands.ChangeEmail uiCommand):this(uiCommand.AccountId, Email.Parse(uiCommand.Email)){}

                public Guid AccountId { get; private set; }
                public Email Email { get; private set; }
            }

            internal class ChangePassword : TransactionalExactlyOnceDeliveryCommand
            {
                [UsedImplicitly] ChangePassword() {}
                internal ChangePassword(Guid accountId, string oldPassword, Password newPassword)
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

            internal class Login : TransactionalExactlyOnceDeliveryCommand<AccountResource.Commands.LogIn.LoginAttemptResult>
            {
                internal Login(Email email, string password)
                {
                    OldContract.Argument(() => email).NotNullOrDefault();
                    OldContract.Argument(() => password).NotNullEmptyOrWhiteSpace();

                    Email = email;
                    Password = password;
                }

                public Email Email { get; }
                public string Password { get; }
            }
        }
    }
}