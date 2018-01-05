using Composable;
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
            public AccountByIdQuery AccountById { get; private set; } = new AccountByIdQuery();

            [TypeId("444153B1-7B35-4F17-9FF3-85040CEEBAAB")]
            public class AccountByIdQuery : EntityByIdQuery<AccountResource, AccountByIdQuery> {}
        }

        public class Command
        {
            public AccountResource.Command.LogIn.UI Login { get; private set; } = new AccountResource.Command.LogIn.UI();
            public AccountResource.Command.Register Register { get; private set; } = new AccountResource.Command.Register();
        }
    }
}
