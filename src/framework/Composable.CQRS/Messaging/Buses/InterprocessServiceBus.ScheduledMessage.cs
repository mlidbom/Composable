using System;
using Composable.System;

namespace Composable.Messaging.Buses
{
    partial class InterprocessServiceBus
    {
        class ScheduledMessage
        {
            public DateTime SendAt { get; }
            public ICommand Message { get; }

            public ScheduledMessage(DateTime sendAt, ICommand message)
            {
                SendAt = sendAt.SafeToUniversalTime();
                Message = message;
            }
        }
    }
}
