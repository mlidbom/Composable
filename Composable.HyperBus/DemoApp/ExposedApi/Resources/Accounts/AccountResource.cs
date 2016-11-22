using Composable.HyperBus.APIDraft;

namespace Composable.HyperBus.DemoApp.ExposedApi.Resources.Accounts
{
    public class AccountResource
    {
        public LinksClass Links { get; }
        public CommandsClass Commands { get; }
        public class LinksClass
        {
            public IQuery<Contact> Contact { get; }
        }

        public class CommandsClass
        {
            public ChangeAccountEmailCommand ChangeEmail(string email) => new ChangeAccountEmailCommand(email);
        }
    }
}