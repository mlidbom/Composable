using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Composable.ServiceBus
{
    public class UnsupportedByNServiceBusException : Exception
    {
        public UnsupportedByNServiceBusException()
            : base("NServiceBus dose not support replay events, please use SynchronousBus do it")
        {

        }
    }
}
