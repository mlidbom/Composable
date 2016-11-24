using Composable.HyperBus.APIDraft;

namespace Composable.HyperBus.DemoApp.ExposedApi.Resources.Accounts
{
    public class ChangeAccountEmailCommand : Command<AccountResource>
    {
        public ChangeAccountEmailCommand(string email) { Email = email; }
        public string Email { get; }
    }
}