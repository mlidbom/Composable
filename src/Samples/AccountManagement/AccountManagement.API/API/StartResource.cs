using Composable;
using Composable.Messaging;
using Composable.Messaging.Buses;

namespace AccountManagement.API
{
    [ClientCacheable(ClientCachingStrategy.CreateNewInstanceWithDefaultConstructor, validForSeconds: int.MaxValue)]
    public class StartResource
    {
        public StartResourceCommands Commands { get; private set; } = new StartResourceCommands();
        public Query Queries { get; private set; } = new Query();

        [ClientCacheable(ClientCachingStrategy.CreateNewInstanceWithDefaultConstructor, validForSeconds: int.MaxValue)]
        public class Query
        {
            public AccountByIdQuery AccountById { get; private set; } = new AccountByIdQuery();

            [TypeId("444153B1-7B35-4F17-9FF3-85040CEEBAAB")]
            public class AccountByIdQuery : EntityByIdQuery<AccountResource, AccountByIdQuery> {}
        }

        [ClientCacheable(ClientCachingStrategy.CreateNewInstanceWithDefaultConstructor, validForSeconds: int.MaxValue)]
        public class StartResourceCommands
        {
            public AccountResource.Command.LogIn.UI Login { get; private set; } = new AccountResource.Command.LogIn.UI();
            public AccountResource.Command.Register.UICommand Register { get; private set; } = new AccountResource.Command.Register.UICommand();
        }
    }
}
