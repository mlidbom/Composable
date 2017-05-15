using Composable.Messaging.Buses;

namespace AccountManagement.UI.QueryModels.DocumentDB.Updaters.ContainerInstallers
{
    static class EventHandlersInstaller
    {
        internal static void Install(IMessageHandlerRegistrar messageHandlerRegistrar)
        {
            messageHandlerRegistrar.Handler<EmailToAccountMapQueryModelUpdater>();
        }
    }
}
