using System;
using Composable.Messaging;

namespace AccountManagement.API
{
    public class StartResource
    {
        public StartResourceCommands Commands { get; private set; } = new StartResourceCommands();
        public StartResourceQueries Queries { get; private set;} = new StartResourceQueries();

        public class StartResourceQueries
        {
            public EntityQuery<AccountResource> AccountById { get; private set; } = new EntityQuery<AccountResource>();
        }

        public class StartResourceCommands
        {
            public AccountResource.Command.LogIn.UI Login { get; private set; } = new AccountResource.Command.LogIn.UI();
            public AccountResource.Command.Register.UICommand Register { get; private set; } = new AccountResource.Command.Register.UICommand();
        }
    }
}
