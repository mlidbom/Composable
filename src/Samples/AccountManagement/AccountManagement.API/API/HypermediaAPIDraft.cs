using System;
using AccountManagement.API.UserCommands;
using AccountManagement.Domain;
using Composable.Messaging;

namespace AccountManagement.API
{
    public static class AccountApi
    {
        public static StartResource Start => new StartResource();
    }

    public class StartResource
    {
        public static IQuery<StartResource> Self => SingletonQuery.For<StartResource>();

        public SingletonQuery<StartResourceCommands> Commands => SingletonQuery.For<StartResourceCommands>();
        public SingletonQuery<StartResourceQueries> Queries => SingletonQuery.For<StartResourceQueries >();

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
        public AccountResource(Guid accountId) : base(accountId) => Commands = new AccountResourceCommands();

        public Email Email { get; set; }
        public Password Password { get; set; }

        public AccountResourceCommands Commands { get; }

        public class AccountResourceCommands
        {}
    }
}
