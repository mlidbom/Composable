using Composable.Messaging;

namespace AccountManagement.API
{
    public class StartResource
    {
        public Command Commands { get; private set; } = new Command();

        public Query Queries { get; private set; } = new Query();

        public class Query
        {
            public BusApi.Remotable.NonTransactional.Queries.EntityLink<AccountResource> AccountById { get; private set; } = new BusApi.Remotable.NonTransactional.Queries.EntityLink<AccountResource>();
        }

        public class Command
        {
            public AccountResource.Command.LogIn Login { get; private set; } = AccountResource.Command.LogIn.Create();
            public AccountResource.Command.Register Register { get; private set; } = AccountResource.Command.Register.Create();
        }
    }
}
