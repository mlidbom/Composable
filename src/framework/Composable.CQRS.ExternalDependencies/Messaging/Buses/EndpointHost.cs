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
        bool _disposed;
        protected readonly List<IEndpoint> Endpoints = new List<IEndpoint>();
        readonly IGlobalBusStrateTracker _globalBusStrateTracker = new GlobalBusStrateTracker();
        readonly InterprocessTransport _interprocessTransport = new InterprocessTransport();

        protected EndpointHost(IRunMode mode) => _mode = mode;

        public static class Production
        {
            public static IEndpointHost Create() => new EndpointHost(RunMode.Production);
        }

        public static class Testing
        {
            public static ITestingEndpointHost BuildHost(Action<ITestingEndpointHost> build,  TestingMode mode = TestingMode.DatabasePool)
            {
                var testingEndpointHost = new TestingEndpointHost(new RunMode(isTesting: true, mode: mode));
                build(testingEndpointHost);
                return testingEndpointHost;
            }
            public static ITestingEndpointHost CreateHost(TestingMode mode = TestingMode.DatabasePool) => new TestingEndpointHost(new RunMode(isTesting: true, mode: mode));
        }

        public IEndpoint RegisterAndStartEndpoint(string name, Action<IEndpointBuilder> setup)
        {
            var builder = new EndpointBuilder(_mode, _globalBusStrateTracker, _interprocessTransport);

            setup(builder);

            var endpoint = builder.Build();
            ConnectEndpoint(endpoint);

            Endpoints.Add(endpoint);

            endpoint.Start();

            return endpoint;
        }

        public void Stop()
        {
            Endpoints.ForEach(endpoint => endpoint.Stop());
        }

        void ConnectEndpoint(IEndpoint endpoint)
        {
            _interprocessTransport.Connect(endpoint);
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
