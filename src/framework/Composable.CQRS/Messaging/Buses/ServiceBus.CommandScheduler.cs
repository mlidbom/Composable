using System;
using System.Collections.Generic;
using System.Threading;
using Composable.GenericAbstractions.Time;
using Composable.System;
using Composable.System.Collections.Collections;
using Composable.System.Linq;
using Composable.System.Threading.ResourceAccess;

namespace Composable.Messaging.Buses
{
    partial class ServiceBus
    {
        class CommandScheduler : IDisposable
        {
            readonly IServiceBus _bus;
            readonly IUtcTimeTimeSource _timeSource;
            Timer _scheduledMessagesTimer;
            readonly List<ScheduledCommand> _scheduledMessages = new List<ScheduledCommand>();
            readonly IGuardedResource _guard = GuardedResource.WithTimeout(1.Seconds());

            public CommandScheduler(IServiceBus bus, IUtcTimeTimeSource timeSource)
            {
                _bus = bus;
                _timeSource = timeSource;
            }

            public void Start() => _guard.Update(() => _scheduledMessagesTimer = new Timer(callback: _ => SendDueCommands(), state: null, dueTime: 0.Seconds(), period: 100.Milliseconds()));

            public void Schedule(DateTime sendAt, ICommand message) => _guard.Update(() =>
            {
                if(_timeSource.UtcNow > sendAt.ToUniversalTime())
                    throw new InvalidOperationException(message: "You cannot schedule a queuedMessageInformation to be sent in the past.");

                _scheduledMessages.Add(new ScheduledCommand(sendAt, message));
            });

            void SendDueCommands() => _guard.Update(() => _scheduledMessages.RemoveWhere(HasPassedSendtime).ForEach(Send));

            bool HasPassedSendtime(ScheduledCommand message) => _timeSource.UtcNow >= message.SendAt;

            void Send(ScheduledCommand scheduledCommand) => _bus.Send(scheduledCommand.Command);

            public void Dispose() => _scheduledMessagesTimer?.Dispose();

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
}
