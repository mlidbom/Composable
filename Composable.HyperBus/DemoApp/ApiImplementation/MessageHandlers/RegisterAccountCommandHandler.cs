using Composable.HyperBus.APIDraft;
using Composable.HyperBus.DemoApp.ExposedApi.Resources.Accounts;

namespace Composable.HyperBus.DemoApp.ApiImplementation.MessageHandlers
{
    public class RegisterAccountCommandHandler : ICommandHandler<RegisterAccountCommand, AccountResource>
    {
        public AccountResource Handle(RegisterAccountCommand command) => (AccountResource)null;
    }
}