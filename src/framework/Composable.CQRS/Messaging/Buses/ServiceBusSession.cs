using System;
using Composable.Messaging.Buses.Implementation;
using Composable.SystemCE.ThreadingCE;
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

        public void Send(IExactlyOnceCommand command)
        {
           RunAssertions(command);
            _transport.SendTransactionally(command);
        }

        public void ScheduleSend(DateTime sendAt, IExactlyOnceCommand command)
        {
            RunAssertions(command);
            _commandScheduler.Schedule(sendAt, command);
        }

        void RunAssertions(IExactlyOnceCommand command)
        {
            _contextGuard.AssertNoContextChangeOccurred(this);
            MessageInspector.AssertValidToSendRemote(command);
            CommandValidator.AssertCommandIsValid(command);
        }
    }
}
