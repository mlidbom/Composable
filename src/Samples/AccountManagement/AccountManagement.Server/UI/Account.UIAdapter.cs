using System;
using AccountManagement.API;
using AccountManagement.Domain;
using AccountManagement.Domain.Passwords;
using AccountManagement.Domain.Registration;
using Composable.Messaging;
using Composable.Messaging.Buses;

namespace AccountManagement.UI
{
    static class AccountUIAdapter
    {
        public static void Login(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommandWithResult(
                (AccountResource.Commands.LogIn.UI logIn, ILocalServiceBusSession bus) =>
                    Account.Login(Email.Parse(logIn.Email), logIn.Password, bus));

            internal static void ChangePassword(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommand(
                (AccountResource.Commands.ChangePassword command, ILocalServiceBusSession busSession) =>
                    busSession.Get(PrivateAccountApi.Queries.ById(command.AccountId)).ChangePassword(command.OldPassword, new Password(command.NewPassword)));

            internal static void ChangeEmail(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommand(
                (AccountResource.Commands.ChangeEmail command, ILocalServiceBusSession bus) =>
                    bus.Get(PrivateAccountApi.Queries.ById(command.AccountId)).ChangeEmail(Email.Parse(command.Email)));

            internal static void Register(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommandWithResult(
                (AccountResource.Commands.Register command, ILocalServiceBusSession bus) =>
                {
                    var (status, account) = Account.Register(command.AccountId, Email.Parse(command.Email), new Password(command.Password), bus);
                    switch(status)
                    {
                        case RegistrationAttemptStatus.Successful:
                            return new AccountResource.Commands.Register.RegistrationAttemptResult(status, new AccountResource(account));
                        case RegistrationAttemptStatus.EmailAlreadyRegistered:
                            return new AccountResource.Commands.Register.RegistrationAttemptResult(status, null);
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                });

            internal static void GetById(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
                (EntityByIdQuery<AccountResource> accountQuery, ILocalServiceBusSession bus)
                    => new AccountResource(bus.Get(PrivateAccountApi.Queries.ReadOnlyCopy(accountQuery.Id))));
    }
}
