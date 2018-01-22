using AccountManagement.API;
using AccountManagement.Domain.Services;
using Composable.Messaging;
using Composable.Messaging.Buses;

namespace AccountManagement.Domain
{
    partial class Account
    {
        internal static class MessageHandlers
        {
            public static void RegisterHandlers(MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
            {
                registrar.ForQuery((EntityByIdQuery<AccountResource> accountQuery, IAccountRepository repository) =>
                                       new AccountResource(repository.GetReadonlyCopy(accountQuery.Id)))
                         .ForCommandWithResult((AccountResource.Commands.Register command, ILocalServiceBusSession bus, IAccountRepository repository) =>
                                                   Account.Register(command.AccountId, Email.Parse(command.Email), new Password(command.Password), repository, bus))
                         .ForCommand((AccountResource.Commands.ChangeEmail command, IAccountRepository repository) =>
                                         repository.Get(command.AccountId).ChangeEmail(Email.Parse(command.Email)))
                         .ForCommand((AccountResource.Commands.ChangePassword command, IAccountRepository repository) =>
                                         repository.Get(command.AccountId).ChangePassword(command.OldPassword, new Password(command.NewPassword)))
                         .ForCommandWithResult((AccountResource.Commands.LogIn.UI logIn, ILocalServiceBusSession bus) =>
                                                   Account.Login(Email.Parse(logIn.Email), logIn.Password, bus));
            }
        }
    }
}
