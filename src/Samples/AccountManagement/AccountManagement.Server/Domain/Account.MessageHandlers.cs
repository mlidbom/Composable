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
                         .ForCommandWithResult((RegisterAccountCommand command, IDuplicateAccountChecker duplicateChecker, IAccountRepository repository) => Register(command, repository, duplicateChecker))
                         .ForCommand((AccountResource.ChangeEmailCommand command, IAccountRepository repository) => repository.Get(command.AccountId).ChangeEmail(Email.Parse(command.Email)));
            }

            static AccountResource Register(RegisterAccountCommand command, IAccountRepository repository, IDuplicateAccountChecker duplicateChecker)
                => new AccountResource(Account.Register(Email.Parse(command.Email), new Password(command.Password), command.AccountId, repository, duplicateChecker));
        }
    }
}
