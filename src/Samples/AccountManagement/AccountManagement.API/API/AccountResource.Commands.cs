using JetBrains.Annotations;

namespace AccountManagement.API
{
    public partial class AccountResource
    {
        public class CommandsCollection
        {
            [UsedImplicitly] CommandsCollection() {}

            public CommandsCollection(AccountResource accountResource)
            {
                ChangeEmail = new Command.ChangeEmail(accountResource.Id);
                ChangePassword = new Command.ChangePassword(accountResource.Id);
            }

            public Command.ChangeEmail ChangeEmail { get; private set; }

            public Command.ChangePassword ChangePassword { get; private set; }
        }
    }
}
