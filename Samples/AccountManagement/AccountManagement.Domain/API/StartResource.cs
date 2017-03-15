using AccountManagement.Domain.Shared;
using Composable.Contracts;
using Composable.Messaging;
using Composable.Messaging.Commands;

namespace AccountManagement.Domain.API
{
    public class StartResource : IResource<StartResource>
    {
        public IQuery<StartResource> Self { get; } =  new SingletonQuery<StartResource>();
    }

    public class AccountResource : EntityResource<AccountResource>
    {
        public AccountResource(Account account) : base(account.Id)
        {
        }
    }

    public class CreateAccountCommand : Command, ICommand<AccountResource>
    {
        Email Email { get; }
        Password Password { get; }

        protected CreateAccountCommand(Email email, Password password)
        {
            Contract.Argument(() => email, () => password)
                    .NotNull();
            Email = email;
            Password = password;
        }
    }
}