using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DependencyInjection;
using Composable.Messaging.Buses.Implementation;
using Composable.System.Linq;

namespace Composable.Messaging.Buses
{
    class EndpointHost : IEndpointHost
    {
        readonly IRunMode _mode;
        readonly Func<IRunMode, IDependencyInjectionContainer> _containerFactory;
        bool _disposed;
        protected readonly List<IEndpoint> Endpoints = new List<IEndpoint>();
        readonly IGlobalBusStrateTracker _globalBusStrateTracker = new GlobalBusStrateTracker();
        readonly InterprocessTransport _interprocessTransport = new InterprocessTransport();

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
            public static ITestingEndpointHost BuildHost(Func<IRunMode, IDependencyInjectionContainer> containerFactory, Action<ITestingEndpointHost> build, TestingMode mode = TestingMode.DatabasePool)
            {
                var testingEndpointHost = new TestingEndpointHost(new RunMode(isTesting: true, mode: mode), containerFactory);
                build(testingEndpointHost);
                return testingEndpointHost;
            }
            public static ITestingEndpointHost CreateHost(Func<IRunMode, IDependencyInjectionContainer> containerFactory, TestingMode mode = TestingMode.DatabasePool) => new TestingEndpointHost(new RunMode(isTesting: true, mode: mode), containerFactory);
        }

        public IEndpoint RegisterAndStartEndpoint(string name, Action<IEndpointBuilder> setup)
        {
            var builder = new EndpointBuilder(_globalBusStrateTracker, _interprocessTransport, _containerFactory(_mode));

            setup(builder);

            var endpoint = builder.Build();

            Endpoints.Add(endpoint);

            endpoint.Start();

            return endpoint;
        }

        public void Stop()
        {
            Endpoints.ForEach(endpoint => endpoint.Stop());
        }


        protected virtual void InternalDispose()
        {
            Stop();
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
