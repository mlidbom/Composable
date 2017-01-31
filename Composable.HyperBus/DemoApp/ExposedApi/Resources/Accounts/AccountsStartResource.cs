using Composable.HyperBus.APIDraft;
// ReSharper disable UnusedMember.Global

namespace Composable.HyperBus.DemoApp.ExposedApi.Resources.Accounts
{
    public class AccountsStartResource
    {
        public static IQuery<AccountsStartResource> Self { get; }
        public CommandsClass Commands { get; } = new CommandsClass();

        public class CommandsClass
        {
            public RegisterAccountCommand Register(string email, string password) => new RegisterAccountCommand();
        }
    }
}