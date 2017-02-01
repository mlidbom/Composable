using Castle.Windsor;
using Composable.HyperBus.APIDraft;
using Composable.HyperBus.DemoApp.ApiImplementation.MessageHandlers;
using Composable.HyperBus.DemoApp.ExposedApi;
using Composable.HyperBus.DemoApp.ExposedApi.Resources.Accounts;

// ReSharper disable All
namespace Composable.HyperBus.DemoApp.ApiImplementation
{
    public class ManualRegistrationApplicationBootstrapper
    {
        public static void RegisterMessageHandlersManually(IMessageHandlerRegistrar registerMessageHandlersFor, IWindsorContainer container)
        {
            registerMessageHandlersFor
                     .Command((RegisterAccountCommand command) => container.Resolve<RegisterAccountCommandHandler>().Handle(command))
                     .Command((ChangeAccountEmailCommand command) => container.Resolve<ChangeAccountEmailCommandHandler>().Handle(command))
                     .Query((EntityQuery<AccountResource> query) => container.Resolve<AccountEntityQueryHandler>().Handle(query))
                     .Event((AccountEvent.IAccountRegisteredEvent @event) => container.Resolve<AccountEmailer>().Handle(@event))
                     .Event((AccountEvent.IAccountEmailChangedEvent @event) => container.Resolve<AccountEmailer>().Handle(@event));
        }
    }
}