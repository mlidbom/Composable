using System;
using Composable.Messaging;

namespace AccountManagement.API
{
    public class StartResource
    {
        public static IQuery<StartResource> Self => SingletonQuery.For<StartResource>();

        public StartResourceCommands Commands => new StartResourceCommands();
        public StartResourceQueries Queries => new StartResourceQueries();

        public class StartResourceQueries
        {
            public EntityQuery<AccountResource> AccountById(Guid accountId) => new EntityQuery<AccountResource>(accountId);
        }

        public class StartResourceCommands
        {
        }
    }
}
