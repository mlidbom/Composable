using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.Messaging.Buses.Implementation;
using Composable.System.Linq;

namespace Composable.Messaging.Buses
{
    class Endpoint : IEndpoint
    {
        readonly EndpointConfiguration _configuration;
        public bool IsRunning { get; private set; }
        public Endpoint(IServiceLocator serviceLocator, EndpointConfiguration configuration)
        {
            Assert.Argument.Assert(serviceLocator != null, configuration != null);
            ServiceLocator = serviceLocator;
            _configuration = configuration;
        }
        public EndpointId Id => _configuration.Id;
        public IServiceLocator ServiceLocator { get; }

        public EndPointAddress Address => _inbox.Address;
        IGlobalBusStateTracker _globalStateTracker;
        IInbox _inbox;
        IInterprocessTransport _transport;
        CommandScheduler _commandScheduler;
        IEndpointRegistry _endpointRegistry;
        IInterprocessTransport _interProcessTransport;

        public async Task InitAsync()
        {
            Assert.State.Assert(!IsRunning);

            IsRunning = true;

            _globalStateTracker = ServiceLocator.Resolve<IGlobalBusStateTracker>();
            _transport = ServiceLocator.Resolve<IInterprocessTransport>();
            _endpointRegistry = ServiceLocator.Resolve<IEndpointRegistry>();
            _interProcessTransport = ServiceLocator.Resolve<IInterprocessTransport>();

            RunSanityChecks();

            var initTasks = new List<Task>()
                            {
                                _transport.StartAsync()
                            };

            //todo: find cleaner way of handling what an endpoint supports
            if(!_configuration.IsPureClientEndpoint)
            {
                _commandScheduler = ServiceLocator.Resolve<CommandScheduler>();
                _inbox = ServiceLocator.Resolve<IInbox>();

                initTasks.Add(_inbox.StartAsync());
                initTasks.Add(_commandScheduler.StartAsync());
            }

            await Task.WhenAll(initTasks);
        }

        public async Task ConnectAsync()
        {
            var serverEndpoints = _endpointRegistry.ServerEndpoints.ToSet();
            if (!_configuration.IsPureClientEndpoint)
            {
                serverEndpoints.Add(Address); //Yes, we do connect to ourselves. Scheduled commands need to dispatch over the remote protocol to get the delivery guarantees...
            }
            await Task.WhenAll(serverEndpoints.Select(address => _interProcessTransport.ConnectAsync(address)));
        }

        static void RunSanityChecks()
        {
            AssertAllTypesNeedingMappingsAreMapped();
        }

        //todo: figure out how to do this sanely
        static void AssertAllTypesNeedingMappingsAreMapped()
        {
        }

        public void Stop()
        {
            Assert.State.Assert(IsRunning);
            IsRunning = false;
            _transport.Stop();
            if(!_configuration.IsPureClientEndpoint)
            {
                _commandScheduler.Stop();
                _inbox.Stop();
            }
        }

        public void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride) => _globalStateTracker?.AwaitNoMessagesInFlight(timeoutOverride);

        public void Dispose()
        {
            if(IsRunning) Stop();
            ServiceLocator.Dispose();
        }
    }
}
