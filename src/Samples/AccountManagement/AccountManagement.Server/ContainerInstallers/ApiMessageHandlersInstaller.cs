using AccountManagement.API;
using AccountManagement.API.UserCommands;
using AccountManagement.Domain;
using AccountManagement.Domain.Services;
using Composable.Messaging;
using Composable.Messaging.Buses;

namespace AccountManagement.ContainerInstallers
{
    static class ApiMessageHandlersInstaller
    {
        public static void RegisterHandlers(MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
        {
            registrar.ForQuery((SingletonQuery<StartResource> query) => new StartResource())
                .ForQuery((EntityQuery<AccountResource> accountQuery, IAccountRepository repository) => new AccountResource(repository.Get(accountQuery.Id).Id))
                .ForCommandWithResult((RegisterAccountCommand command, IDuplicateAccountChecker duplicateChecker, IAccountRepository repository) =>
                     {
                         Account.Register(Email.Parse(command.Email), new Password(command.Password), command.AccountId, repository, duplicateChecker);
                         return new AccountResource(command.AccountId);
                     })
                ;
        }
    }
}
