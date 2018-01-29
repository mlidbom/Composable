using Composable.System.Reflection;

namespace Composable.Messaging.Buses.Implementation
{
    static class IMessageExtensions
    {
        internal static bool RequiresResponse(this BusApi.IMessage @this) => @this is BusApi.IQuery || @this is BusApi.Remote.AtMostOnce.ICommand || @this.GetType().Implements(typeof(BusApi.Remote.ICommand<>));
    }
}
