using Composable.SystemCE;
using NetMQ;

namespace Composable.Messaging.Buses.Implementation
{
    static class NetMqSocketExtensions
    {
        internal static string BindAndReturnActualAddress(this NetMQSocket @this, string address)
        {
            //Check if we are auto-assigning a port with a port wildcard
            if(address.StartsWithInvariant("tcp://") && ( address.EndsWithInvariant(":0") || address.EndsWithInvariant(":*")))
            {
                var startOfAddress = address[0..^2];
                var port = @this.BindRandomPort(startOfAddress);
                return $"{startOfAddress}:{port}";
            }

            @this.Bind(address);
            return address;
        }
    }
}
