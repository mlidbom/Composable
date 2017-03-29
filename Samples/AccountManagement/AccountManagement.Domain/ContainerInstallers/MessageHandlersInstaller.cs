using AccountManagement.Domain.API;
using AccountManagement.Domain.Events.PropertyUpdated;
using AccountManagement.Domain.QueryModels.Updaters;
using Composable.DependencyInjection;
using Composable.Messaging;
using Composable.Messaging.Buses;

namespace AccountManagement.Domain.ContainerInstallers
{
    static class MessageHandlersInstaller
    {
        internal static void Install(IDependencyInjectionContainer container)
        {
            container.Register(
                               Component.For<EmailToAccountMapQueryModelUpdater>()
                                         .ImplementedBy<EmailToAccountMapQueryModelUpdater>()
                                         .LifestyleScoped()
                              );

            container.CreateServiceLocator().Use<IMessageHandlerRegistrar>(registrar =>
                                                        registrar
                                                            .ForEvent<IAccountEmailPropertyUpdatedEvent>(@event => container.CreateServiceLocator().Use<EmailToAccountMapQueryModelUpdater>(updater => updater.Handle(@event)))
                                                            .ForQuery<IQuery<StartResource>, StartResource>(query => new StartResource()));
        }
    }
}
