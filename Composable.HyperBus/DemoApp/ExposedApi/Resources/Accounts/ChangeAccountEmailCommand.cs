using Composable.HyperBus.APIDraft;

// ReSharper disable All
namespace Composable.HyperBus.DemoApp.ExposedApi.Resources.Accounts
{
    public class ChangeAccountEmailCommand : Command<AccountResource>
    {
        public ChangeAccountEmailCommand(string email) { Email = email; }
        public string Email { get; }
    }
}