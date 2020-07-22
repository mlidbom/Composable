using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Composable.GenericAbstractions.Time;
using Composable.Logging;
using Composable.System;
using Composable.System.Collections.Collections;
using Composable.System.Linq;
using Composable.System.Threading;
using Composable.System.Threading.ResourceAccess;
using Composable.System.Transactions;

namespace Composable.Messaging.Buses.Implementation
{
    class CommandScheduler : IDisposable
    {
        readonly IOutbox _transport;
        readonly IUtcTimeTimeSource _timeSource;
        readonly ITaskRunner _taskRunner;
        Timer? _scheduledMessagesTimer;
        readonly List<ScheduledCommand> _scheduledMessages = new List<ScheduledCommand>();
        readonly IResourceGuard _guard = ResourceGuard.WithTimeout(1.Seconds());

        public CommandScheduler(IOutbox transport, IUtcTimeTimeSource timeSource, ITaskRunner taskRunner)
        {
            _transport = transport;
            _timeSource = timeSource;
            _taskRunner = taskRunner;
        }

        public async Task StartAsync()
        {
            _scheduledMessagesTimer = new Timer(callback: _ => SendDueCommands(), state: null, dueTime: 0.Seconds(), period: 100.Milliseconds());
            await Task.CompletedTask.NoMarshalling();
        }

        public void Schedule(DateTime sendAt, MessageTypes.Remotable.ExactlyOnce.ICommand message) => _guard.Update(() =>
        {
            if(_timeSource.UtcNow > sendAt.ToUniversalTimeSafely())
                throw new InvalidOperationException(message: "You cannot schedule a queuedMessageInformation to be sent in the past.");

            var scheduledCommand = new ScheduledCommand(sendAt, message);
            //todo:Persistence.
            _scheduledMessages.Add(scheduledCommand);
        });

        void SendDueCommands() => _guard.Update(() => _scheduledMessages.RemoveWhere(HasPassedSendtime).ForEach(Send));

        bool HasPassedSendtime(ScheduledCommand message) => _timeSource.UtcNow >= message.SendAt;

        void Send(ScheduledCommand scheduledCommand) => _taskRunner.RunAndSurfaceExceptions(() => TransactionScopeCe.Execute(() => _transport.SendTransactionally(scheduledCommand.Command)));

        public void Dispose() => Stop();

        public void Stop() { _scheduledMessagesTimer?.Dispose(); }

        class ScheduledCommand
        {
            public DateTime SendAt { get; }
            public MessageTypes.Remotable.ExactlyOnce.ICommand Command { get; }

            public ScheduledCommand(DateTime sendAt, MessageTypes.Remotable.ExactlyOnce.ICommand command)
            {
                SendAt = sendAt.ToUniversalTimeSafely();
                Command = command;
            }
        }
    }
}
