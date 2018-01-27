using System;
using AccountManagement.API;
using AccountManagement.Domain;
using AccountManagement.Domain.Events;
using AccountManagement.Domain.Passwords;
using AccountManagement.Domain.Registration;
using Composable.Functional;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Persistence.EventStore;

namespace AccountManagement.UI
{
    static class AccountUIAdapter
    {
        public static void Login(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommandWithResult(
            (AccountResource.Command.LogIn logIn, ILocalServiceBusSession bus) =>
            {
                var email = Email.Parse(logIn.Email);

                if(AccountApi.Queries.TryGetByEmail(email).GetLocalOn(bus) is Some<Account> account)
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
            (AccountResource.Command.ChangePassword command, ILocalServiceBusSession bus) =>
                AccountApi.Queries.GetForUpdate(command.AccountId).GetLocalOn(bus).ChangePassword(command.OldPassword, new Password(command.NewPassword)));

        internal static void ChangeEmail(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommand(
            (AccountResource.Command.ChangeEmail command, ILocalServiceBusSession bus) =>
                AccountApi.Queries.GetForUpdate(command.AccountId).GetLocalOn(bus).ChangeEmail(Email.Parse(command.Email)));

        internal static void Register(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommandWithResult(
            (AccountResource.Command.Register command, ILocalServiceBusSession bus) =>
            {
                var (status, account) = Account.Register(command.AccountId, Email.Parse(command.Email), new Password(command.Password), bus);
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
            (RemoteEntityResourceQuery<AccountResource> accountQuery, ILocalServiceBusSession bus)
                => new AccountResource(AccountApi.Queries.GetReadOnlyCopy(accountQuery.EntityId).GetLocalOn(bus)));
    }
}
