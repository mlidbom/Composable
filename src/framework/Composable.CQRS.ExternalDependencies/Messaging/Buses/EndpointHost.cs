using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DependencyInjection;
using Composable.System.Linq;

namespace Composable.Messaging.Buses
{
    class EndpointHost : IEndpointHost
    {
        readonly IRunMode _mode;
        bool _disposed;
        protected readonly List<IEndpoint> Endpoints = new List<IEndpoint>();
        readonly IGlobalBusStrateTracker _globalBusStrateTracker = new GlobalBusStrateTracker();

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
            var builder = new EndpointBuilder(name, _mode, _globalBusStrateTracker);

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
            var myRegistry = endpoint.ServiceLocator.Resolve<MessageHandlerRegistry>();
            var myCommandHandlers = myRegistry._commandHandlers.ToArray();
            var myQueryHandlers = myRegistry._queryHandlers.ToArray();
            var myEventRegistrations = myRegistry._eventHandlerRegistrations.ToArray();

            var registryWithAllAlreadyCrossConnectedHandlers = Endpoints.FirstOrDefault()?.ServiceLocator.Resolve<MessageHandlerRegistry>();
            if(registryWithAllAlreadyCrossConnectedHandlers != null)
            {
                registryWithAllAlreadyCrossConnectedHandlers._commandHandlers.ForEach(handler => myRegistry._commandHandlers.Add(handler.Key, handler.Value));
                registryWithAllAlreadyCrossConnectedHandlers._eventHandlerRegistrations.ForEach(registration => myRegistry._eventHandlerRegistrations.Add(registration));
                registryWithAllAlreadyCrossConnectedHandlers._queryHandlers.ForEach(handler => myRegistry._queryHandlers.Add(handler.Key, handler.Value));
            }

            foreach(var registryWithoutMyHandlers in Endpoints.Select(endpointWithoutMyHandlers => endpointWithoutMyHandlers.ServiceLocator.Resolve<MessageHandlerRegistry>()))
            {
                myCommandHandlers.ForEach(handler => registryWithoutMyHandlers._commandHandlers.Add(handler.Key, handler.Value));
                myEventRegistrations.ForEach(registration => registryWithoutMyHandlers._eventHandlerRegistrations.Add(registration));
                myQueryHandlers.ForEach(handler => registryWithoutMyHandlers._queryHandlers.Add(handler.Key, handler.Value));
            }
        }


        protected virtual void InternalDispose()
        {
            Stop();
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
