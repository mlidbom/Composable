using System;
using AccountManagement.API;
using AccountManagement.Domain;
using AccountManagement.Domain.Events;
using AccountManagement.Domain.Passwords;
using AccountManagement.Domain.Registration;
using Composable.Functional;
using Composable.Messaging;
using Composable.Messaging.Buses;

namespace AccountManagement.UI
{
    static class AccountUIAdapter
    {
        public static void Login(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommandWithResult(
            (AccountResource.Command.LogIn logIn, ILocalServiceBusSession busSession) =>
            {
                var email = Email.Parse(logIn.Email);

                if(busSession.Execute(AccountApi.Queries.TryGetByEmail(email)) is Some<Account> account)
                {
                    switch(account.Value.Login(logIn.Password))
                    {
                        case AccountEvent.LoggedIn loggedIn:
                            return AccountResource.Command.LogIn.LoginAttemptResult.Success(loggedIn.AuthenticationToken);
                        case AccountEvent.LoginFailed _:
                            return AccountResource.Command.LogIn.LoginAttemptResult.Failure();
                        default: throw new ArgumentOutOfRangeException();
                    }
                } else
                {
                    return AccountResource.Command.LogIn.LoginAttemptResult.Failure();
                }
            });

        internal static void ChangePassword(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommand(
            (AccountResource.Command.ChangePassword command, ILocalServiceBusSession busSession) =>
                busSession.Execute(AccountApi.Queries.ById(command.AccountId)).ChangePassword(command.OldPassword, new HashedPassword(command.NewPassword)));

        internal static void ChangeEmail(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommand(
            (AccountResource.Command.ChangeEmail command, ILocalServiceBusSession bus) =>
                bus.Execute(AccountApi.Queries.ById(command.AccountId)).ChangeEmail(Email.Parse(command.Email)));

        internal static void Register(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommandWithResult(
            (AccountResource.Command.Register command, ILocalServiceBusSession bus) =>
            {
                var (status, account) = Account.Register(command.AccountId, Email.Parse(command.Email), new HashedPassword(command.Password), bus);
                switch(status)
                {
                    case RegistrationAttemptStatus.Successful:
                        return new AccountResource.Command.Register.RegistrationAttemptResult(status, new AccountResource(account));
                    case RegistrationAttemptStatus.EmailAlreadyRegistered:
                        return new AccountResource.Command.Register.RegistrationAttemptResult(status, null);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });

        internal static void GetById(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
            (AggregateLink<AccountResource> accountQuery, ILocalServiceBusSession bus)
                => new AccountResource(bus.Execute(AccountApi.Queries.ReadOnlyCopy(accountQuery.Id))));
    }
}
