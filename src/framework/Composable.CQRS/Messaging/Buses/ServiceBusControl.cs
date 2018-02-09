using System.Collections.Generic;
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
        readonly EndpointConfiguration _configuration;
        bool _started;

        public ServiceBusControl(IInterprocessTransport transport, IInbox inbox, CommandScheduler commandScheduler, EndpointConfiguration configuration)
        {
            _transport = transport;
            _inbox = inbox;
            _commandScheduler = commandScheduler;
            _configuration = configuration;
        }

        async Task IServiceBusControl.StartAsync()
        {
            Assert.State.Assert(!_started);

            _started = true;

            var initTasks = new List<Task>()
                                   {
                                       _transport.StartAsync()
                                   };

            if(!_configuration.IsPureClientEndpoint)
            {
                initTasks.Add(_inbox.StartAsync());
                initTasks.Add(_commandScheduler.StartAsync());
            }

            await Task.WhenAll(initTasks);
        }

        void IServiceBusControl.Stop()
        {
            Assert.State.Assert(_started);
            _started = false;
            _transport.Stop();
            if(!_configuration.IsPureClientEndpoint)
            {
                _commandScheduler.Stop();
                _inbox.Stop();
            }
        }
    }
}
