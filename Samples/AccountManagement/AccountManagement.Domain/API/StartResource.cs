using AccountManagement.Domain.Shared;
using Composable.Contracts;
using Composable.Messaging;
using Composable.Messaging.Commands;

namespace AccountManagement.Domain.API
{
    public class StartResource : IResource<StartResource>
    {
        public IQuery<StartResource> Self { get; } = new SingletonQuery<StartResource>();

        public StartResourceCommands Commands = new StartResourceCommands();

        public class StartResourceCommands
        {
            public CreateAccountCommand CreateAccount(Email email, Password password) => new CreateAccountCommand(email, password);
        }
    }

    public class AccountResource : EntityResource<AccountResource>
    {
        public AccountResource(Account account) : base(account.Id) => Commands = new AccountResourceCommands(account);

        AccountResourceCommands Commands { get; }
        public class AccountResourceCommands
        {
            public AccountResourceCommands(Account account)
            {
            }
        }
    }

    public class CreateAccountCommand : Command, ICommand<AccountResource>
    {
        Email Email { get; }
        Password Password { get; }

        internal CreateAccountCommand(Email email, Password password)
        {
            Contract.Argument(() => email, () => password)
                    .NotNull();
            Email = email;
            Password = password;
        }
    }
}
