using Composable.HyperBus.APIDraft;
using Composable.HyperBus.DemoApp.ExposedApi;
using Composable.HyperBus.DemoApp.ExposedApi.Resources.Accounts;

namespace Composable.HyperBus.DemoApp.ApiImplementation
{
    public class ApplicationBootstrapper
    {
        public static void RegisterMessageHandlers(IMessageHandlerRegistrar registerMessageHandlers)
        {
            registerMessageHandlers
                     .Command((RegisterAccountCommand command) => (AccountResource)null)
                     .Command((ChangeAccountEmailCommand command) => { })
                     .Query((EntityQuery<AccountResource> query) => (AccountResource)null)
                     .Event((AccountEvent.IAccountRegisteredEvent @event) => {})
                     .Event((AccountEvent.IAccountEmailChangedEvent @event) => {});
        }
    }
}