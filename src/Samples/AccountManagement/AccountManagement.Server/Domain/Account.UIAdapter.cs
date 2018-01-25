using AccountManagement.API;
using AccountManagement.Domain.Passwords;
using Composable.Messaging;
using Composable.Messaging.Buses;

namespace AccountManagement.Domain
{
    partial class Account
    {
        internal static class UIAdapter
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
                    Account.Register(command.AccountId, Email.Parse(command.Email), new Password(command.Password), bus));

            internal static void GetById(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
                (EntityByIdQuery<AccountResource> accountQuery, ILocalServiceBusSession bus)
                    => new AccountResource(bus.Get(PrivateAccountApi.Queries.ReadOnlyCopy(accountQuery.Id))));
        }
    }
}
