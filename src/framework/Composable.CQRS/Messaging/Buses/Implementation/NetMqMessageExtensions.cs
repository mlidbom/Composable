using System;
using NetMQ;

namespace Composable.Messaging.Buses.Implementation
{
    static class NetMqMessageExtensions
    {
        internal static void Append(this NetMQMessage @this, Guid guid) => @this.Append(guid.ToByteArray());
    }
}
