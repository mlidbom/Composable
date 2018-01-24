using Composable.Contracts;
using Composable.Messaging.Buses.Implementation;
using JetBrains.Annotations;

namespace Composable.Messaging.Buses
{
    [UsedImplicitly] class ServiceBusControl : IServiceBusControl
    {
        readonly IInterprocessTransport _transport;
        readonly IInbox _inbox;
        readonly CommandScheduler _commandScheduler;
        bool _started;

        public ServiceBusControl(IInterprocessTransport transport, IInbox inbox, CommandScheduler commandScheduler)
        {
            _transport = transport;
            _inbox = inbox;
            _commandScheduler = commandScheduler;
        }

        void IServiceBusControl.Start()
        {
            Contract.State.Assert(!_started);

            _started = true;

            _inbox.Start();
            _transport.Start();
            _commandScheduler.Start();
        }

        void IServiceBusControl.Stop()
        {
            Contract.State.Assert(_started);
            _started = false;
            _commandScheduler.Stop();
            _transport.Stop();
            _inbox.Stop();
        }
    }
}
