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
                registrar.ForQuery((EntityQuery<AccountResource> accountQuery, IAccountRepository repository) =>
                                       new AccountResource(repository.GetReadonlyCopy(accountQuery.Id)))
                         .ForCommandWithResult((AccountResource.Command.Register.UICommand command, IFindAccountByEmail duplicateChecker, IAccountRepository repository) =>
                                                   new AccountResource(Account.Register(command.ToDomainCommand(), repository, duplicateChecker)))
                         .ForCommand((AccountResource.Command.ChangeEmail.UI command, IAccountRepository repository) =>
                                         repository.Get(command.AccountId).ChangeEmail(command.ToDomainCommand()))
                         .ForCommand((AccountResource.Command.ChangePassword.UI command, IAccountRepository repository) =>
                                         repository.Get(command.AccountId).ChangePassword(command.ToDomainCommand()))
                         .ForCommandWithResult((AccountResource.Command.LogIn.UI logIn, IInProcessServiceBus bus) =>
                                                   Account.Login(logIn.ToDomainCommand(), bus));
            }
        }
    }
}
