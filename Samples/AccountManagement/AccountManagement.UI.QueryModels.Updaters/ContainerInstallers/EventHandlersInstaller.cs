using AccountManagement.Domain.Events;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;

namespace AccountManagement.UI.QueryModels.DocumentDB.Updaters.ContainerInstallers
{
    static class EventHandlersInstaller
    {
        internal static void Install(IDependencyInjectionContainer container)
        {
            container.Register(
                               Component.For<EmailToAccountMapQueryModelUpdater>()
                                         .ImplementedBy<EmailToAccountMapQueryModelUpdater>()
                                         .LifestyleScoped()
                              );

            container.CreateServiceLocator().Use<IMessageHandlerRegistrar>(
                registrar => registrar.ForEvent<AccountEvent.PropertyUpdated.Email>(
                    @event => container.CreateServiceLocator().Use<EmailToAccountMapQueryModelUpdater>(updater => updater.Handle(@event))));
        }
    }
}
