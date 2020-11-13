using Newtonsoft.Json;

namespace AccountManagement.API
{
    public partial class AccountResource
    {
        public class CommandsCollection
        {
#pragma warning disable IDE0051 // Remove unused private members
            [JsonConstructor]CommandsCollection(Command.ChangeEmail changeEmail, Command.ChangePassword changePassword)
#pragma warning restore IDE0051 // Remove unused private members
            {
                ChangeEmail = changeEmail;
                ChangePassword = changePassword;
            }

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
