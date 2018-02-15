using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.Messaging.Buses.Implementation;
using Composable.Refactoring.Naming;
using Composable.System.Linq;
using Composable.System.Threading;

namespace Composable.Messaging.Buses
{
    public class EndpointHost : IEndpointHost
    {
        readonly IRunMode _mode;
        readonly Func<IRunMode, IDependencyInjectionContainer> _containerFactory;
        bool _disposed;
        protected readonly List<IEndpoint> Endpoints = new List<IEndpoint>();
        internal IGlobalBusStateTracker GlobalBusStateTracker;

        protected EndpointHost(IRunMode mode, Func<IRunMode, IDependencyInjectionContainer> containerFactory)
        {
            _mode = mode;
            _containerFactory = containerFactory;
            GlobalBusStateTracker = new NullOpGlobalBusStateTracker();
        }

        public static class Production
        {
            public static IEndpointHost Create(Func<IRunMode, IDependencyInjectionContainer> containerFactory) => new EndpointHost(RunMode.Production, containerFactory);
        }

        public static class Testing
        {
            public static ITestingEndpointHost Create(Func<IRunMode, IDependencyInjectionContainer> containerFactory, TestingMode mode = TestingMode.DatabasePool) => new TestingEndpointHost(new RunMode(isTesting: true, testingMode: mode), containerFactory);
        }

        public IEndpoint RegisterEndpoint(string name, EndpointId id, Action<IEndpointBuilder> setup) => InternalRegisterEndpoint(new EndpointConfiguration(name, id, _mode, isPureClientEndpoint: false), setup);

        IEndpoint InternalRegisterEndpoint(EndpointConfiguration configuration, Action<IEndpointBuilder> setup)
        {
            using(var builder = new EndpointBuilder(this, GlobalBusStateTracker, _containerFactory(_mode), configuration))
            {
                setup(builder);

                var endpoint = builder.Build();

                Endpoints.Add(endpoint);
                return endpoint;
            }
        }

        static readonly EndpointConfiguration ClientEndpointConfiguration = new EndpointConfiguration(name: $"{nameof(TestingEndpointHost)}_Default_Client_Endpoint",
                                                                                                      id: new EndpointId(Guid.Parse("D4C869D2-68EF-469C-A5D6-37FCF2EC152A")),
                                                                                                      mode: RunMode.Production,
                                                                                                      isPureClientEndpoint: true);

        public IEndpoint RegisterClientEndpoint(Action<IEndpointBuilder> setup) => InternalRegisterEndpoint(ClientEndpointConfiguration, setup);

        bool _isStarted;

        public async Task StartAsync()
        {
            Assert.State.Assert(!_isStarted, Endpoints.None(endpoint => endpoint.IsRunning));
            _isStarted = true;

            await Task.WhenAll(Endpoints.Select(endpointToStart => endpointToStart.InitAsync())).NoMarshalling();
            await Task.WhenAll(Endpoints.Select(endpointToStart => endpointToStart.ConnectAsync())).NoMarshalling();
        }

        public void Start() => StartAsync().WaitUnwrappingException();

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
