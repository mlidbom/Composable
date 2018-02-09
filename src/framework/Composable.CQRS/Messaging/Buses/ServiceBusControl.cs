using System.Threading.Tasks;
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

        async Task IServiceBusControl.StartAsync()
        {
            Assert.State.Assert(!_started);

            _started = true;

            await Task.WhenAll(_inbox.StartAsync(),
                               _transport.StartAsync(),
                               _commandScheduler.StartAsync());
        }

        void IServiceBusControl.Stop()
        {
            Assert.State.Assert(_started);
            _started = false;
            _commandScheduler.Stop();
            _transport.Stop();
            _inbox.Stop();
        }
    }
}
