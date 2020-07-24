using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.Messaging.Buses.Implementation;
using Composable.SystemCE.CollectionsCE.GenericCE;
using Composable.SystemCE.ThreadingCE;

namespace Composable.Messaging.Buses
{
    class Endpoint : IEndpoint
    {
        class ServerComponents : IDisposable
        {
            readonly CommandScheduler _commandScheduler;
            public readonly IInbox Inbox;
            readonly IOutbox _outbox;

            public ServerComponents(CommandScheduler commandScheduler, IInbox inbox, IOutbox outbox)
            {
                _commandScheduler = commandScheduler;
                Inbox = inbox;
                _outbox = outbox;
            }

            public async Task InitAsync() => await Task.WhenAll(Inbox.StartAsync(), _commandScheduler.StartAsync(), _outbox.StartAsync()).NoMarshalling();
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
                        ITransport transport,
                        IEndpointRegistry endpointRegistry,
                        EndpointConfiguration configuration)
        {
            Contract.ArgumentNotNull(serviceLocator, nameof(serviceLocator), configuration, nameof(configuration));
            ServiceLocator = serviceLocator;
            _globalStateTracker = globalStateTracker;
            _transport = transport;
            _configuration = configuration;
            _endpointRegistry = endpointRegistry;
        }
        public EndpointId Id => _configuration.Id;
        public IServiceLocator ServiceLocator { get; }

        public EndPointAddress? Address => _serverComponents?.Inbox.Address;
        readonly IGlobalBusStateTracker _globalStateTracker;
        readonly ITransport _transport;
        readonly IEndpointRegistry _endpointRegistry;

        ServerComponents? _serverComponents;

        public async Task InitAsync()
        {
            Assert.State.Assert(!IsRunning);

            RunSanityChecks();

            //todo: find cleaner way of handling what an endpoint supports
            if(!_configuration.IsPureClientEndpoint)
            {
                _serverComponents = new ServerComponents(ServiceLocator.Resolve<CommandScheduler>(), ServiceLocator.Resolve<IInbox>(), ServiceLocator.Resolve<IOutbox>());

                await _serverComponents.InitAsync().NoMarshalling();
            }


            IsRunning = true;
        }

        public async Task ConnectAsync()
        {
            var serverEndpoints = _endpointRegistry.ServerEndpoints.ToSet();
            if (_serverComponents != null)
            {
                serverEndpoints.Add(_serverComponents.Inbox.Address); //Yes, we do connect to ourselves. Scheduled commands need to dispatch over the remote protocol to get the delivery guarantees...
            }
            await Task.WhenAll(serverEndpoints.Select(address => _transport.ConnectAsync(address))).NoMarshalling();
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
