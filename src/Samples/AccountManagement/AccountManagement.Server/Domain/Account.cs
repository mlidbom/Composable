using System;
using AccountManagement.API;
using AccountManagement.Domain.Events;
using AccountManagement.Domain.Services;
using Composable.Contracts;
using Composable.Functional;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.Persistence.EventStore.AggregateRoots;

namespace AccountManagement.Domain
{
    ///Completely encapsulates all the business logic for an account.  Should make it impossible for clients to use the class incorrectly.
    partial class Account : AggregateRoot<Account, AccountEvent.Implementation.Root, AccountEvent.Root>, IAccountResourceData
    {
        public Email Email { get; private set; } //Never public setters on an aggregate.
        public Password Password { get; private set; } //Never public setters on an aggregate.

        //No public constructors please. Aggregates are created through domain verbs.
        //Expose named factory methods that ensure the instance is valid instead. See register method below.
        Account() : base(new DateTimeNowTimeSource())
        {
            //Maintain correct state as events are raised or read from the store.
            //Use property updated events whenever possible. Changes to public state should be represented by property updated events.
            RegisterEventAppliers()
                .For<AccountEvent.PropertyUpdated.Email>(e => Email = e.Email)
                .For<AccountEvent.PropertyUpdated.Password>(e => Password = e.Password)
                .IgnoreUnhandled<AccountEvent.LoggedIn>()
                .IgnoreUnhandled<AccountEvent.LoginFailed>();
        }

        //Ensure that the state of the instance is sane. If not throw an exception.
        //Called after every call to Publish.
        protected override void AssertInvariantsAreMet() => OldContract.Invariant(() => Email, () => Password, () => Id).NotNullOrDefault();

        /// <summary><para>Used when a user manually creates an account themselves.</para>
        /// <para>Note how this design with a named static creation method: </para>
        /// <para> * makes it clearer what the caller intends.</para>
        /// <para> * makes it impossible to use the class incorrectly, such as forgetting to check for duplicates or save the new instance in the repository.</para>
        /// <para> * reduces code duplication since multiple callers are not burdened with saving the instance, checking for duplicates etc.</para>
        /// </summary>
        static AccountResource.Command.Register.RegistrationAttemptResult Register(Command.Register command, IAccountRepository repository, IInProcessServiceBus bus)
        {
            //Ensure that it is impossible to call with invalid arguments.
            //Since all domain types should ensure that it is impossible to create a non-default value that is invalid we only have to disallow default values.
            OldContract.Argument(() => command, () => repository, () => bus).NotNullOrDefault();

            //The email is the unique identifier for logging into the account so obviously duplicates are forbidden.
            if(bus.Query(PrivateApi.Account.Queries.TryGetByEmail(command.Email)).HasValue)
            {
                return AccountResource.Command.Register.RegistrationAttemptResult.EmailAlreadyRegistered;
            }

            var newAccount = new Account();
            newAccount.Publish(new AccountEvent.Implementation.UserRegistered(accountId: command.AccountId, email: command.Email, password: command.Password));
            repository.Add(newAccount);

            return AccountResource.Command.Register.RegistrationAttemptResult.Successful;
        }

        void ChangePassword(Command.ChangePassword command)
        {
            OldContract.Argument(() => command).NotNullOrDefault();

            Password.AssertIsCorrectPassword(command.OldPassword);

            Publish(new AccountEvent.Implementation.UserChangedPassword(command.NewPassword));
        }

        void ChangeEmail(Command.ChangeEmail command)
        {
            OldContract.Argument(() => command).NotNullOrDefault();

            Publish(new AccountEvent.Implementation.UserChangedEmail(command.Email));
        }

        AccountResource.Command.LogIn.LoginAttemptResult Login(string logInPassword)
        {
            if(Password.IsCorrectPassword(logInPassword))
            {
                var authenticationToken = Guid.NewGuid().ToString();
                Publish(new AccountEvent.Implementation.LoggedIn(authenticationToken));
                return AccountResource.Command.LogIn.LoginAttemptResult.Success(authenticationToken);
            }

            Publish(new AccountEvent.Implementation.LoginFailed());
            return AccountResource.Command.LogIn.LoginAttemptResult.Failure();
        }

        static AccountResource.Command.LogIn.LoginAttemptResult Login(Command.Login logIn, IInProcessServiceBus bus) =>
            bus.Query(PrivateApi.Account.Queries.TryGetByEmail(logIn.Email)) is Option<Account>.Some account
                ? account.Value.Login(logIn.Password)
                : AccountResource.Command.LogIn.LoginAttemptResult.Failure();
    }
}
