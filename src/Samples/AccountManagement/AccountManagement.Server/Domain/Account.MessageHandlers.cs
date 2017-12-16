using AccountManagement.API;
using AccountManagement.API.UserCommands;
using AccountManagement.Domain.Services;
using Composable.Messaging;
using Composable.Messaging.Buses;

namespace AccountManagement.Domain
{
    public partial class Account
    {
        internal static class MessageHandlers
        {
            public static void RegisterHandlers(MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
            {
                registrar.ForQuery((SingletonQuery<StartResource> query) => new StartResource())
                         .ForQuery((EntityQuery<AccountResource> accountQuery, IAccountRepository repository) => new AccountResource(repository.Get(accountQuery.Id)))
                         .ForCommandWithResult((RegisterAccountCommand command, IDuplicateAccountChecker duplicateChecker, IAccountRepository repository)
                                                   => new AccountResource(Register(Email.Parse(command.Email), new Password(command.Password), command.AccountId, repository, duplicateChecker)));
            }
        }
    }
}
