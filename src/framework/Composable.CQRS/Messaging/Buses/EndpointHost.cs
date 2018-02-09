using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.Messaging.Buses.Implementation;
using Composable.Refactoring.Naming;
using Composable.System.Linq;

namespace Composable.Messaging.Buses
{
    public class EndpointHost : IEndpointHost
    {
        readonly IRunMode _mode;
        readonly Func<IRunMode, IDependencyInjectionContainer> _containerFactory;
        bool _disposed;
        protected readonly List<IEndpoint> Endpoints = new List<IEndpoint>();
        internal readonly IGlobalBusStateTracker GlobalBusStateTracker = new GlobalBusStateTracker();

        protected EndpointHost(IRunMode mode, Func<IRunMode, IDependencyInjectionContainer> containerFactory)
        {
            _mode = mode;
            _containerFactory = containerFactory;
        }

        public static class Production
        {
            public static IEndpointHost Create(Func<IRunMode, IDependencyInjectionContainer> containerFactory) => new EndpointHost(RunMode.Production, containerFactory);
        }

        public static class Testing
        {
            public static ITestingEndpointHost Create(Func<IRunMode, IDependencyInjectionContainer> containerFactory, TestingMode mode = TestingMode.DatabasePool) => new TestingEndpointHost(new RunMode(isTesting: true, testingMode: mode), containerFactory);
        }

        public IEndpoint RegisterEndpoint(string name, EndpointId id, Action<IEndpointBuilder> setup)
        {
            var builder = new EndpointBuilder(GlobalBusStateTracker, _containerFactory(_mode), name, id);

            setup(builder);

            var endpoint = builder.Build();

            Endpoints.Add(endpoint);
            return endpoint;
        }

        public IEndpoint RegisterClientEndpointForRegisteredEndpoints()
        {
            var clientEndpoint = RegisterEndpoint($"{nameof(TestingEndpointHost)}_Default_Client_Endpoint", new EndpointId(Guid.Parse("D4C869D2-68EF-469C-A5D6-37FCF2EC152A")), _ => {});

            var typeMapper = clientEndpoint.ServiceLocator.Resolve<TypeMapper>();

            Endpoints.Where(endpoint => endpoint != clientEndpoint)
                     .Select(otherEndpoint => otherEndpoint.ServiceLocator.Resolve<TypeMapper>())
                     .ForEach(otherTypeMapper => typeMapper.IncludeMappingsFrom(otherTypeMapper));

            return clientEndpoint;
        }

        bool _isStarted;
        public void Start()
        {
            Assert.State.Assert(!_isStarted, Endpoints.None(endpoint => endpoint.IsRunning));
            _isStarted = true;

            //performance: Client endpoints do not need message storage and other endpoints need not connect to client endpoints.
            Task.WaitAll(Endpoints.Select(endpointToStart => endpointToStart.InitAsync()).ToArray());

            var endpointsWithRemoteMessageHandlers = Endpoints
                                                    .Where(endpoint => endpoint.ServiceLocator.Resolve<IMessageHandlerRegistry>().HandledRemoteMessageTypeIds().Any())
                                                    .Select(@this => @this.Address)
                                                    .ToList();

            Endpoints.ForEach(endpointToStart => endpointToStart.Connect(endpointsWithRemoteMessageHandlers));
        }

        public void Stop()
        {
            Assert.State.Assert(_isStarted);
            _isStarted = false;
            Endpoints.Where(endpoint => endpoint.IsRunning).ForEach(endpoint => endpoint.Stop());
        }

        protected virtual void InternalDispose()
        {
            if(_isStarted)
            {
                Stop();
            }

            Endpoints.ForEach(endpoint => endpoint.Dispose());
        }

        public void Dispose()
        {
            if(!_disposed)
            {
                _disposed = true;
                InternalDispose();
                GC.SuppressFinalize(this);
            }
        }
    }
}
