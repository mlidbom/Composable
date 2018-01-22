

// ReSharper disable MemberCanBeMadeStatic.Global

using System;
using Composable.Messaging;
using Composable.Messaging.Buses;

namespace AccountManagement.API
{
    /// <summary>
    /// This class provides the ability to use type safe API navigation from a type that does not run on .Net. For instance via Typescript in browser.
    /// We generate typescript interfaces for each of the resources exposed via the Queries and commands ultimately reachable through the Start Query.
    /// A generic browser type can the be used to navigate the whole API remotely.
    /// For .Net clients the next class in this file is a far more convenient way to consume the API.
    /// </summary>
    public static class AccountWebClientApi
    {
        public static readonly SelfGeneratingResourceQuery<StartResource> Start = SelfGeneratingResourceQuery<StartResource>.Instance;
    }


    /// <summary>
    /// This is the entry point to the API for all .Net clients. It provides a simple intuitive fluent API for accessing all of the functionality in the AccountManagement application.
    /// </summary>
    public static class AccountApi
    {
        static readonly NavigationSpecification<StartResource> Start = NavigationSpecification.Get(AccountWebClientApi.Start);

        public static class Query
        {
            static readonly NavigationSpecification<StartResource.Query> Queries = Start.Select(start => start.Queries);

            public static NavigationSpecification<AccountResource> AccountById(Guid accountId) => Queries.GetRemote(@this => @this.AccountById.WithId(accountId));
        }

        public static class Command
        {
            static readonly NavigationSpecification<StartResource.Command> Commands = Start.Select(start => start.Commands);

            public static NavigationSpecification<AccountResource.Command.Register> Register() => Commands.Select(@this => @this.Register);
            public static NavigationSpecification<AccountResource.Command.Register.RegistrationAttemptResult> Register(Guid accountId, string email, string password) => Commands.Post(_ => new AccountResource.Command.Register(accountId, email, password));
        }
    }
}
