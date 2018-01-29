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
            (AccountResource.Command.LogIn logIn, ILocalApiNavigatorSession bus) =>
            {
                var email = Email.Parse(logIn.Email);

                if(bus.Execute(AccountApi.Queries.TryGetByEmail(email)) is Some<Account> account)
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
            (AccountResource.Command.ChangePassword command, ILocalApiNavigatorSession bus) =>
                bus.Execute(AccountApi.Queries.GetForUpdate(command.AccountId)).ChangePassword(command.OldPassword, new Password(command.NewPassword)));

        internal static void ChangeEmail(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommand(
            (AccountResource.Command.ChangeEmail command, ILocalApiNavigatorSession bus) =>
                bus.Execute(AccountApi.Queries.GetForUpdate(command.AccountId)).ChangeEmail(Email.Parse(command.Email)));

        internal static void Register(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommandWithResult(
            (AccountResource.Command.Register command, ILocalApiNavigatorSession bus) =>
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
            (BusApi.Remotable.Query.RemoteEntityResourceQuery<AccountResource> accountQuery, ILocalApiNavigatorSession bus)
                => new AccountResource(bus.Execute(AccountApi.AccountQueryModel.Queries.Get(accountQuery.EntityId))));
    }
}
