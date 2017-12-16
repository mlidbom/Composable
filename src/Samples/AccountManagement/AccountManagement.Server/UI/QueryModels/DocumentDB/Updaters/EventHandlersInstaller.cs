using AccountManagement.Domain.Events;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;

namespace AccountManagement.UI.QueryModels.DocumentDB.Updaters.ContainerInstallers
{
    static class EventHandlersInstaller
    {
        internal static void Install(IDependencyInjectionContainer container)
        {
            container.Register(Component.For<EmailToAccountMapQueryModelUpdater>().ImplementedBy<EmailToAccountMapQueryModelUpdater>().LifestyleScoped());
        }

        internal static void Install(IMessageHandlerRegistrar messageHandlerRegistrar, IServiceLocator serviceLocator)
        {
            messageHandlerRegistrar.ForEvent<AccountEvent.PropertyUpdated.Email>(@event => serviceLocator.Use<EmailToAccountMapQueryModelUpdater>(updater => updater.Handle(@event)));
        }
    }
}
