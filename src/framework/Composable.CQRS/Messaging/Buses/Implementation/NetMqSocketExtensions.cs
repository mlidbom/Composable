using NetMQ;

namespace Composable.Messaging.Buses.Implementation
{
    static class NetMqSocketExtensions
    {
        internal static string BindAndReturnActualAddress(this NetMQSocket @this, string address)
        {
            //Check if we are autoassigning a port with a port wildcard
            if(address.StartsWith("tcp://") && ( address.EndsWith(":0") || address.EndsWith(":*")))
            {
                var startOfAddress = address.Substring(0, address.Length - 2);
                var port = @this.BindRandomPort(startOfAddress);
                return $"{startOfAddress}:{port}";
            }

            @this.Bind(address);
            return address;
        }
    }
}
