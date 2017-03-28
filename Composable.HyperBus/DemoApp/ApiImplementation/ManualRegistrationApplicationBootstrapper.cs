using Composable.DependencyInjection;
using Composable.HyperBus.APIDraft;
using Composable.HyperBus.DemoApp.ApiImplementation.MessageHandlers;
using Composable.HyperBus.DemoApp.ExposedApi;
using Composable.HyperBus.DemoApp.ExposedApi.Resources.Accounts;

// ReSharper disable All
namespace Composable.HyperBus.DemoApp.ApiImplementation
{
    public class ManualRegistrationApplicationBootstrapper
    {
        public static void RegisterMessageHandlersManually(IMessageHandlerRegistrar registerMessageHandlersFor, IServiceLocator serviceLocator)
        {
            registerMessageHandlersFor
                     .Command((RegisterAccountCommand command) => serviceLocator.Resolve<RegisterAccountCommandHandler>().Handle(command))
                     .Command((ChangeAccountEmailCommand command) => serviceLocator.Resolve<ChangeAccountEmailCommandHandler>().Handle(command))
                     .Query((EntityQuery<AccountResource> query) => serviceLocator.Resolve<AccountEntityQueryHandler>().Handle(query))
                     .Event((AccountEvent.IAccountRegisteredEvent @event) => serviceLocator.Resolve<AccountEmailer>().Handle(@event))
                     .Event((AccountEvent.IAccountEmailChangedEvent @event) => serviceLocator.Resolve<AccountEmailer>().Handle(@event));
        }
    }
}