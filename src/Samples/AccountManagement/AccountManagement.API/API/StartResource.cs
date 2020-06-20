using Composable.Messaging;
using JetBrains.Annotations;

namespace AccountManagement.API
{
    [UsedImplicitly] public class StartResource
    {
        public Command Commands { get; private set; } = new Command();

        public Query Queries { get; private set; } = new Query();

        public class Query
        {
            public MessageTypes.Remotable.NonTransactional.Queries.EntityLink<AccountResource> AccountById { get; private set; } = new MessageTypes.Remotable.NonTransactional.Queries.EntityLink<AccountResource>();
        }

        public class Command
        {
            public AccountResource.Command.LogIn Login { get; private set; } = AccountResource.Command.LogIn.Create();
            public AccountResource.Command.Register Register { get; private set; } = AccountResource.Command.Register.Create();
        }
    }
}
