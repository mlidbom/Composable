using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
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
        bool _running;

        protected EndpointHost(IRunMode mode) => _mode = mode;

        public static class Production
        {
            public static IEndpointHost Create() => new EndpointHost(RunMode.Production);
        }

        public static class Testing
        {
            public static ITestingEndpointHost CreateHost(TestingMode mode = TestingMode.DatabasePool) => new TestingEndpointHost(new RunMode(isTesting: true, mode: mode));
        }

        public IEndpoint RegisterEndpoint(string name, Action<IEndpointBuilder> setup)
        {
            Contract.Assert.That(!_running, "!_running");
            var builder = new EndpointBuilder(name, _mode, _globalBusStrateTracker);

            setup(builder);

            var endpoint = builder.Build();
            ConnectEndpoint(endpoint);

            Endpoints.Add(endpoint);

            return endpoint;
        }

        public void Start()
        {
            Contract.Assert.That(!_running, "!_running");
            _running = true;
            Endpoints.ForEach(endpoint => endpoint.Start());
        }
        public void Stop()
        {
            _running = false;
            Endpoints.ForEach(endpoint => endpoint.Stop());
        }

        void ConnectEndpoint(IEndpoint endpoint)
        {
            var currentRegistry = endpoint.ServiceLocator.Resolve<MessageHandlerRegistry>();
            var currentCommandHandlers = currentRegistry._commandHandlers.ToArray();
            var currentQueryHandlers = currentRegistry._queryHandlers.ToArray();
            var currentEventRegistrations = currentRegistry._eventHandlerRegistrations.ToArray();

            foreach(var otherEndpoint in Endpoints)
            {
                var otherRegistry = otherEndpoint.ServiceLocator.Resolve<MessageHandlerRegistry>();

                otherRegistry._commandHandlers.ForEach(handler => currentRegistry._commandHandlers.Add(handler.Key, handler.Value));
                otherRegistry._eventHandlerRegistrations.ForEach(registration => currentRegistry._eventHandlerRegistrations.Add(registration));
                otherRegistry._queryHandlers.ForEach(handler => currentRegistry._queryHandlers.Add(handler.Key, handler.Value));

                currentCommandHandlers.ForEach(handler => otherRegistry._commandHandlers.Add(handler.Key, handler.Value));
                currentEventRegistrations.ForEach(registration => otherRegistry._eventHandlerRegistrations.Add(registration));
                currentQueryHandlers.ForEach(handler => otherRegistry._queryHandlers.Add(handler.Key, handler.Value));
            }
        }


        protected virtual void InternalDispose()
        {
            _disposed = true;
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
