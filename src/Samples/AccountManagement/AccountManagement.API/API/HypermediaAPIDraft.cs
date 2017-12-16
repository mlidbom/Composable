using System;
using AccountManagement.API.UserCommands;
using AccountManagement.Domain;
using Composable.Messaging;
using JetBrains.Annotations;

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
            public RegisterAccountCommand CreateAccount => new RegisterAccountCommand();
        }
    }

    public class AccountResource : EntityResource<AccountResource>
    {
        [UsedImplicitly]AccountResource() {}

        internal AccountResource(IAccountResourceData account) : base(account.Id)
        {
            Commands = new AccountResourceCommands();
            Email = account.Email;
            Password = account.Password;
        }

        public Email Email { get; private set; }
        public Password Password { get; private set; }

        public AccountResourceCommands Commands { get; private set; }

        public class AccountResourceCommands
        {}
    }


    interface IAccountResourceData
    {
        Guid Id { get; }
        Email Email { get; }
        Password Password { get; }
    }
}
