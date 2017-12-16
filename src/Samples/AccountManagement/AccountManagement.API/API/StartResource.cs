using System;
using AccountManagement.Domain;
using Composable.Contracts;
using Composable.Messaging;
using Composable.Messaging.Commands;

// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global

namespace AccountManagement.API
{
    public class StartResource
    {
        public IQuery<StartResource> Self { get; } = new SingletonQuery<StartResource>();

        public StartResourceCommands Commands = new StartResourceCommands();

        public class StartResourceCommands
        {
            public CreateAccountCommand CreateAccount(Email email, Password password) => new CreateAccountCommand(email, password);
        }

        public Guid MessageId { get; } = Guid.NewGuid();
    }

    public class AccountResource : EntityResource<AccountResource>
    {
        public AccountResource(Guid accountId) : base(accountId) => Commands = new AccountResourceCommands(accountId);

        AccountResourceCommands Commands { get; }

        class AccountResourceCommands
        {
            internal AccountResourceCommands(Guid accountId)
            {
            }
        }
    }

    public class CreateAccountCommand : DomainCommand, IDomainCommand<AccountResource>
    {
        Email Email { get; }
        Password Password { get; }

        internal CreateAccountCommand(Email email, Password password)
        {
            OldContract.Argument(() => email, () => password)
                    .NotNull();
            Email = email;
            Password = password;
        }
    }
}
