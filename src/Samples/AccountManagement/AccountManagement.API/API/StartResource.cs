using Composable.Messaging;
using Composable.Messaging.Buses;

namespace AccountManagement.API
{
    [ClientCacheable(ClientCachingStrategy.CreateNewInstanceWithDefaultConstructor, validForSeconds: int.MaxValue)]
    public class StartResource
    {
        public StartResourceCommands Commands { get; private set; } = new StartResourceCommands();
        public StartResourceQueries Queries { get; private set;} = new StartResourceQueries();

        [ClientCacheable(ClientCachingStrategy.CreateNewInstanceWithDefaultConstructor, validForSeconds: int.MaxValue)]
        public class StartResourceQueries
        {
            public EntityQuery<AccountResource> AccountById { get; private set; } = new EntityQuery<AccountResource>();
        }

        [ClientCacheable(ClientCachingStrategy.CreateNewInstanceWithDefaultConstructor, validForSeconds: int.MaxValue)]
        public class StartResourceCommands
        {
            public AccountResource.Command.LogIn.UI Login { get; private set; } = new AccountResource.Command.LogIn.UI();
            public AccountResource.Command.Register.UICommand Register { get; private set; } = new AccountResource.Command.Register.UICommand();
        }
    }
}
