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


        public static void RegisterHandlers(IMessageHandlerRegistrar registrar, IServiceLocator serviceLocator)
        {
            registrar.RegisterEventHandler<AccountEvent.PropertyUpdated.Email>(@event => serviceLocator.Use<EmailToAccountMapQueryModelUpdater>(updater => updater.Handle(@event)));
        }
    }
}
