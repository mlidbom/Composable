using System;
using JetBrains.Annotations;

namespace AccountManagement.API
{
    public partial class AccountResource
    {
        public CommandsCollection CommandsCollections { get; private set; }
        public class CommandsCollection
        {
            public Guid AccountId { get; private set; }

            [UsedImplicitly] CommandsCollection() {}

            public CommandsCollection(AccountResource accountResource) => AccountId = accountResource.Id;

            public Command.ChangeEmail.UI ChangeEmail => new Command.ChangeEmail.UI(AccountId);

            public Command.ChangePassword.UI ChangePassword(string oldPassword, string newPassword) => new Command.ChangePassword.UI()
                                                                                                   {
                                                                                                       AccountId = AccountId,
                                                                                                       OldPassword = oldPassword,
                                                                                                       NewPassword = newPassword
                                                                                                   };
        }
    }
}
