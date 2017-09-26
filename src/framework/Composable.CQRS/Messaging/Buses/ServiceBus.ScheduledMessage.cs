using System;
using Composable.System;

namespace Composable.Messaging.Buses
{
    partial class ServiceBus
    {
        class ScheduledCommand
        {
            public DateTime SendAt { get; }
            public ICommand Command { get; }

            public ScheduledCommand(DateTime sendAt, ICommand command)
            {
                SendAt = sendAt.SafeToUniversalTime();
                Command = command;
            }
        }
    }
}
