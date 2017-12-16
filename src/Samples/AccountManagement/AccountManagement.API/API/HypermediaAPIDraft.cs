using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AccountManagement.API.UserCommands;
using AccountManagement.API.ValidationAttributes;
using AccountManagement.Domain;
using Composable.Messaging;
using Composable.Messaging.Commands;
using JetBrains.Annotations;
using Newtonsoft.Json;
// ReSharper disable MemberCanBeMadeStatic.Global

namespace AccountManagement.API
{
    public static class AccountApi
    {
        public static StartResource Start => new StartResource();
    }

    public class StartResource
    {
        public static IQuery<StartResource> Self => SingletonQuery.For<StartResource>();

        public StartResourceCommands Commands => new StartResourceCommands();
        public StartResourceQueries Queries => new StartResourceQueries();

        public class StartResourceQueries
        {
            public EntityQuery<AccountResource> AccountById(Guid accountId) => new EntityQuery<AccountResource>(accountId);
        }

        public class StartResourceCommands
        {
            public RegisterAccountCommand CreateAccount(Guid accountId, string email, string password) => new RegisterAccountCommand(accountId, email, password);
        }
    }

    public class AccountResource : EntityResource<AccountResource>
    {
        [UsedImplicitly] AccountResource() {}

        internal AccountResource(IAccountResourceData account) : base(account.Id)
        {
            Commands = new AccountResourceCommands(this);
            Email = account.Email;
            Password = account.Password;
        }

        public Email Email { get; private set; }
        public Password Password { get; private set; }

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
            [Required][Email] public string Email { get; set; }
        }
    }

    interface IAccountResourceData
    {
        Guid Id { get; }
        Email Email { get; }
        Password Password { get; }
    }
}
