using AccountManagement.Domain.Shared;
using Composable.Contracts;
using Composable.Messaging;
using Composable.Messaging.Commands;
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable ClassNeverInstantiated.Global

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

        class AccountResourceCommands
        {
            internal AccountResourceCommands(Account account)
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
