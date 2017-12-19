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
                registrar.ForQuery((EntityQuery<AccountResource> accountQuery, IAccountRepository repository) => new AccountResource(repository.GetReadonlyCopy(accountQuery.Id)));

                registrar.ForCommandWithResult((AccountResource.Command.Register.DomainCommand command, IDuplicateAccountChecker duplicateChecker, IAccountRepository repository) => new AccountResource(Account.Register(command, repository, duplicateChecker)));

                registrar.ForCommand((AccountResource.ChangeEmailUICommand command, IAccountRepository repository) => repository.Get(command.AccountId).ChangeEmail(Email.Parse(command.Email)))
                    .ForCommand((AccountResource.ChangePasswordUICommand command, IAccountRepository repository) =>
                                    repository.Get(command.AccountId).ChangePassword(command.OldPassword, new Password(command.NewPassword)));
            }
        }
    }
}
