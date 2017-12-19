using JetBrains.Annotations;

namespace AccountManagement.API
{
    public partial class AccountResource
    {
        public CommandsCollection CommandsCollections { get; private set; }
        public class CommandsCollection
        {
            [UsedImplicitly] CommandsCollection() {}

            public CommandsCollection(AccountResource accountResource)
            {
                ChangeEmail = new Command.ChangeEmail.UI(accountResource.Id);
                ChangePassword = new Command.ChangePassword.UI(accountResource.Id);
            }

            public Command.ChangeEmail.UI ChangeEmail { get; private set; }

            public Command.ChangePassword.UI ChangePassword { get; private set; }
        }
    }
}
