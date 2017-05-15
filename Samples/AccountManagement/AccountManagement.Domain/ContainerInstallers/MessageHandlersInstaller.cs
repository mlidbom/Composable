using AccountManagement.Domain.QueryModels.Updaters;
using Composable.Messaging.Buses;

namespace AccountManagement.Domain.ContainerInstallers
{
    static class MessageHandlersInstaller
    {
        internal static void Install(IMessageHandlerRegistrar messageHandlerRegistrar)
        {
            messageHandlerRegistrar.Handler<EmailToAccountMapQueryModelUpdater>();
        }
    }
}
