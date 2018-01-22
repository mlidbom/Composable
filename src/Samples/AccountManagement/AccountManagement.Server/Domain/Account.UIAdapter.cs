using AccountManagement.API;
using AccountManagement.Domain.Services;
using Composable.Messaging;
using Composable.Messaging.Buses;

namespace AccountManagement.Domain
{
    partial class Account
    {
        internal static class UIAdapter
        {
            public static void RegisterHandlers(MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
            {
                GetAccount(registrar);
                RegisterAccount(registrar);
                ChangeEmail(registrar);
                ChangePassword(registrar);
                Login(registrar);
            }

            static void Login(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommandWithResult(
                (AccountResource.Commands.LogIn.UI logIn, ILocalServiceBusSession bus) =>
                    Account.Login(Email.Parse(logIn.Email), logIn.Password, bus));

            static void ChangePassword(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommand(
                (AccountResource.Commands.ChangePassword command, IAccountRepository repository) =>
                    repository.Get(command.AccountId).ChangePassword(command.OldPassword, new Password(command.NewPassword)));

            static void ChangeEmail(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommand(
                (AccountResource.Commands.ChangeEmail command, IAccountRepository repository) =>
                    repository.Get(command.AccountId).ChangeEmail(Email.Parse(command.Email)));

            static void RegisterAccount(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommandWithResult(
                (AccountResource.Commands.Register command, ILocalServiceBusSession bus, IAccountRepository repository) =>
                    Register(command.AccountId, Email.Parse(command.Email), new Password(command.Password), repository, bus));

            static void GetAccount(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
                (EntityByIdQuery<AccountResource> accountQuery, IAccountRepository repository)
                    => new AccountResource(repository.GetReadonlyCopy(accountQuery.Id)));
        }
    }
}
