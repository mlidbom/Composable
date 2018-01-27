using Composable.System.Reflection;

namespace Composable.Messaging.Buses.Implementation
{
    static class IMessageExtensions
    {
        internal static bool RequiresResponse(this MessagingApi.IMessage @this) => @this is MessagingApi.IQuery || @this.GetType().Implements(typeof(MessagingApi.Remote.ExactlyOnce.ICommand<>));
    }
}
