using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Composable.GenericAbstractions.Time;
using Composable.System;
using Composable.System.Collections.Collections;
using Composable.System.Linq;
using Composable.System.Threading.ResourceAccess;

namespace Composable.Messaging.Buses.Implementation
{
    class CommandScheduler : IDisposable
    {
        readonly IInterprocessTransport _transport;
        readonly IUtcTimeTimeSource _timeSource;
        Timer _scheduledMessagesTimer;
        readonly List<ScheduledCommand> _scheduledMessages = new List<ScheduledCommand>();
        readonly IResourceGuard _guard = ResourceGuard.WithTimeout(1.Seconds());

        public CommandScheduler(IInterprocessTransport transport, IUtcTimeTimeSource timeSource)
        {
            _transport = transport;
            _timeSource = timeSource;
        }

        public void Start() => _guard.Update(() => _scheduledMessagesTimer = new Timer(callback: _ => SendDueCommands(), state: null, dueTime: 0.Seconds(), period: 100.Milliseconds()));

        public async Task Schedule(DateTime sendAt, ITransactionalExactlyOnceDeliveryCommand message) => await _guard.Update(async () =>
        {
            if(_timeSource.UtcNow > sendAt.ToUniversalTime())
                throw new InvalidOperationException(message: "You cannot schedule a queuedMessageInformation to be sent in the past.");

            var scheduledCommand = new ScheduledCommand(sendAt, message);
            await PersistAsync(scheduledCommand);
            _scheduledMessages.Add(scheduledCommand);
        });


        async Task PersistAsync(ScheduledCommand scheduledCommand) => await Task.CompletedTask;

        void SendDueCommands() => _guard.Update(() => _scheduledMessages.RemoveWhere(HasPassedSendtime).ForEach(Send));

        bool HasPassedSendtime(ScheduledCommand message) => _timeSource.UtcNow >= message.SendAt;

        void Send(ScheduledCommand scheduledCommand) => _transport.DispatchAsync(scheduledCommand.Command).Wait();

        public void Dispose() => Stop();

        public void Stop() { _scheduledMessagesTimer?.Dispose(); }

        class ScheduledCommand
        {
            public DateTime SendAt { get; }
            public ITransactionalExactlyOnceDeliveryCommand Command { get; }

            public ScheduledCommand(DateTime sendAt, ITransactionalExactlyOnceDeliveryCommand command)
            {
                SendAt = sendAt.SafeToUniversalTime();
                Command = command;
            }
        }
    }
}
