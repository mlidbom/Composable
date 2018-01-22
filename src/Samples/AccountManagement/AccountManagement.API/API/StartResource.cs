using Composable.Messaging;
using Composable.Messaging.Buses;

namespace AccountManagement.API
{
    public class StartResource : ISelfGeneratingResource
    {
        public Command Commands { get; private set; } = new Command();

        public Query Queries { get; private set; } = new Query();

        public class Query
        {
            public EntityByIdQuery<AccountResource> AccountById { get; private set; } = new EntityByIdQuery<AccountResource>();
        }

        public class Command
        {
            public AccountResource.Commands.LogIn.UI Login { get; private set; } = new AccountResource.Commands.LogIn.UI();
            public AccountResource.Commands.Register Register { get; private set; } = new AccountResource.Commands.Register();
        }
    }
}
