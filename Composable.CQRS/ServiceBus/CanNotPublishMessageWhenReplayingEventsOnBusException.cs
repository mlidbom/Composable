using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Composable.System;

namespace Composable.ServiceBus
{
    public class CanNotPublishMessageWhenReplayingEventsOnBusException : Exception
    {
        public CanNotPublishMessageWhenReplayingEventsOnBusException(object message)
            : base("Can not send/publish message when replaying events on SynchronousBus, message type:{0}".FormatWith(message.GetType()))
        {
        }
    }
}
