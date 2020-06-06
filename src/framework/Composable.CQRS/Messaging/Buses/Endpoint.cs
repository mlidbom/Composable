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
        class ServerComponents : IDisposable
        {
            readonly CommandScheduler _commandScheduler;
            public readonly IInbox Inbox;

            public ServerComponents(CommandScheduler commandScheduler, IInbox inbox)
            {
                _commandScheduler = commandScheduler;
                Inbox = inbox;
            }

            public async Task InitAsync() => await Task.WhenAll(Inbox.StartAsync(), _commandScheduler.StartAsync());
            public void Stop()
            {
                _commandScheduler.Stop();
                Inbox.Stop();
            }

            public void Dispose()
            {
                _commandScheduler.Dispose();
            }
        }

        readonly EndpointConfiguration _configuration;
        public bool IsRunning { get; private set; }
        public Endpoint(IServiceLocator serviceLocator,
                        IGlobalBusStateTracker globalStateTracker,
                        IInterprocessTransport transport,
                        IEndpointRegistry endpointRegistry,
                        IInterprocessTransport interProcessTransport,
                        EndpointConfiguration configuration)
        {
            Assert.Argument.Assert(serviceLocator != null, configuration != null);
            ServiceLocator = serviceLocator;
            _globalStateTracker = globalStateTracker;
            _transport = transport;
            _configuration = configuration;
            _endpointRegistry = endpointRegistry;
            _interProcessTransport = interProcessTransport;
        }
        public EndpointId Id => _configuration.Id;
        public IServiceLocator ServiceLocator { get; }

        public EndPointAddress? Address => _serverComponents?.Inbox.Address;
        readonly IGlobalBusStateTracker _globalStateTracker;
        readonly IInterprocessTransport _transport;
        readonly IEndpointRegistry _endpointRegistry;
        readonly IInterprocessTransport _interProcessTransport;

        ServerComponents? _serverComponents;

        public async Task InitAsync()
        {
            Assert.State.Assert(!IsRunning);

            RunSanityChecks();

            var initTasks = new List<Task>
                            {
                                _transport.StartAsync()
                            };

            //todo: find cleaner way of handling what an endpoint supports
            if(!_configuration.IsPureClientEndpoint)
            {
                _serverComponents = new ServerComponents(ServiceLocator.Resolve<CommandScheduler>(), ServiceLocator.Resolve<IInbox>());

                initTasks.Add(_serverComponents.InitAsync());
            }

            await Task.WhenAll(initTasks);

            IsRunning = true;
        }

        public async Task ConnectAsync()
        {
            var serverEndpoints = _endpointRegistry.ServerEndpoints.ToSet();
            if (_serverComponents != null)
            {
                serverEndpoints.Add(_serverComponents.Inbox.Address); //Yes, we do connect to ourselves. Scheduled commands need to dispatch over the remote protocol to get the delivery guarantees...
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
            _serverComponents?.Stop();
        }

        public void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride) => _globalStateTracker.AwaitNoMessagesInFlight(timeoutOverride);

        public void Dispose()
        {
            if(IsRunning) Stop();
            ServiceLocator.Dispose();
            _serverComponents?.Dispose();
        }
    }
}
