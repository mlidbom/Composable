using AccountManagement.Domain.Events;
using AccountManagement.Domain.QueryModels.Updaters;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;

namespace AccountManagement.Domain.ContainerInstallers
{
    static class MessageHandlersInstaller
    {
        internal static void SetupContainer(IDependencyInjectionContainer container)
        {
            container.Register(Component.For<EmailToAccountMapQueryModelUpdater>().ImplementedBy<EmailToAccountMapQueryModelUpdater>().LifestyleScoped());
        }


        public static void RegisterHandlers(MessageHandlerRegistrarWithDependencyInjectionSupport registrar, IServiceLocator serviceLocator)
        {
            registrar.ForEvent((AccountEvent.PropertyUpdated.Email @event, EmailToAccountMapQueryModelUpdater updater) => updater.Handle(@event));
        }
    }
}
