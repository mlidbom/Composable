using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Composable.System;

namespace Composable.ServiceBus
{
    public class CanNotPublishMessageWhenReplayingException : Exception
    {
        public CanNotPublishMessageWhenReplayingException(object message)
            : base("Can not send/publish message when replaying, message type:{0}".FormatWith(message.GetType()))
        {
        }
    }
}
