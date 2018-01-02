using Composable.System.Reflection;

namespace Composable.Messaging.Buses.Implementation
{
    static class IMessageExtensions
    {
        internal static bool RequiresResponse(this IMessage @this) => @this is IQuery || @this.GetType().Implements(typeof(ITransactionalExactlyOnceDeliveryCommand<>));
    }
}
