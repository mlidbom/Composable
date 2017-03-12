using Composable.HyperBus.APIDraft;
using Composable.HyperBus.DemoApp.ExposedApi.Resources.Accounts;

namespace Composable.HyperBus.DemoApp.ApiImplementation.MessageHandlers
{
    class ChangeAccountEmailCommandHandler : ICommandHandler<ChangeAccountEmailCommand, AccountResource>
    {
        public AccountResource Handle(ChangeAccountEmailCommand command) => null;
    }
}