using JetBrains.Annotations;

namespace AccountManagement.API
{
    public partial class AccountResource
    {
        public CommandsCollection Command { get; private set; }
        public class CommandsCollection
        {
            [UsedImplicitly] CommandsCollection() {}

            public CommandsCollection(AccountResource accountResource)
            {
                ChangeEmail = new Commands.ChangeEmail(accountResource.Id);
                ChangePassword = new Commands.ChangePassword(accountResource.Id);
            }

            public Commands.ChangeEmail ChangeEmail { get; private set; }

            public Commands.ChangePassword ChangePassword { get; private set; }
        }
    }
}
