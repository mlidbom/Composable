using Composable.HyperBus.APIDraft;
using Composable.HyperBus.DemoApp.ExposedApi.Resources.Accounts;

namespace Composable.HyperBus.DemoApp.ApiImplementation.MessageHandlers
{
    public class AccountEntityQueryHandler : IQueryHandler<EntityQuery<AccountResource>, AccountResource>
    {
        public AccountResource Handle(EntityQuery<AccountResource> query) => (AccountResource)null;
    }
}