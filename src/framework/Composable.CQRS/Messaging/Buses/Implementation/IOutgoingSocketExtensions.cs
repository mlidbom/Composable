using NetMQ;

namespace Composable.Messaging.Buses.Implementation
{
    // ReSharper disable once InconsistentNaming
    static class IOutgoingSocketExtensions
    {
        public static void Send(this IOutgoingSocket @this, TransportMessage.OutGoing message) => message.Send(@this);
        public static void Send(this IOutgoingSocket @this, TransportMessage.Response.Outgoing message) => message.Send(@this);
    }
}
