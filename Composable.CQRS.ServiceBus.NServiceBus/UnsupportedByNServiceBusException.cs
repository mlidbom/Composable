using System;

namespace Composable.CQRS.ServiceBus.NServiceBus
{
    public class UnsupportedByNServiceBusException : Exception
    {
        public UnsupportedByNServiceBusException()
            : base("NServiceBus dose not support replay events, please use SynchronousBus to replay")
        {

        }
    }
}
