using Composable.Messaging.Buses.Implementation;
using NetMQ;

namespace Composable.Messaging.NetMQCE
{
    static class NetMQSocketCE
    {
        public static void Connect(this NetMQSocket @this, EndPointAddress address) => @this.Connect(address.StringValue);
    }
}
