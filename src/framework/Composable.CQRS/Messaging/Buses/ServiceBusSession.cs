using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.Messaging.Hypermedia;
using Composable.SystemExtensions.Threading;
using JetBrains.Annotations;

namespace Composable.Messaging.Buses
{
    [UsedImplicitly] class ServiceBusSession : IServiceBusSession
    {
        readonly IOutbox _transport;
        readonly CommandScheduler _commandScheduler;
        readonly ISingleContextUseGuard _contextGuard;

        public ServiceBusSession(IOutbox transport, CommandScheduler commandScheduler)
        {
            _contextGuard = new CombinationUsageGuard(new SingleTransactionUsageGuard());
            _transport = transport;
            _commandScheduler = commandScheduler;
        }

        public void Send(MessageTypes.Remotable.ExactlyOnce.ICommand command)
        {
           RunAssertions(command);
            _transport.SendIfTransactionCommits(command);
        }

        public void ScheduleSend(DateTime sendAt, MessageTypes.Remotable.ExactlyOnce.ICommand command)
        {
            RunAssertions(command);
            _commandScheduler.Schedule(sendAt, command);
        }

        void RunAssertions(MessageTypes.Remotable.ExactlyOnce.ICommand command)
        {
            _contextGuard.AssertNoContextChangeOccurred(this);
            MessageInspector.AssertValidToSendRemote(command);
            CommandValidator.AssertCommandIsValid(command);
        }
    }
}
