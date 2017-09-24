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
        readonly List<IEndpoint> _endpoints = new List<IEndpoint>();

        protected EndpointHost(IRunMode mode) => _mode = mode;

        public static class Production
        {
            public static IEndpointHost Create() => new EndpointHost(RunMode.Production);
        }

        public static class Testing
        {
            public static ITestingEndpointHost CreateHost(TestingMode mode = TestingMode.DatabasePool) => new TestingEndpointHost(new RunMode(isTesting: true, mode: mode));
        }

        public IEndpoint RegisterEndpoint(Action<IEndpointBuilder> setup)
        {
            var builder = new EndpointBuilder(_mode);

            setup(builder);

            var endpoint = builder.Build();
            ConnectEndpoint(endpoint);

            _endpoints.Add(endpoint);

            return endpoint;
        }

        public void Start() => _endpoints.ForEach(endpoint => endpoint.Start());
        public void Stop() => _endpoints.ForEach(endpoint => endpoint.Stop());

        void ConnectEndpoint(IEndpoint endpoint)
        {
            var currentRegistry = endpoint.ServiceLocator.Resolve<MessageHandlerRegistry>();
            var currentCommandHandlers = currentRegistry._commandHandlers.ToArray();
            var currentQueryHandlers = currentRegistry._queryHandlers.ToArray();
            var currentEventRegistrations = currentRegistry._eventHandlerRegistrations.ToArray();

            foreach(var otherEndpoint in _endpoints)
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

        public void Dispose()
        {
            if(!_disposed)
            {
                _disposed = true;
                Stop();

                var exceptions = _endpoints
                    .SelectMany(endpoint => endpoint.ServiceLocator
                                                    .Resolve<TestingOnlyInterprocessServiceBus>().ThrownExceptions)
                    .ToList();

                if(exceptions.Any())
                {
                    throw new AggregateException("Unhandled exceptions thrown in bus", exceptions.ToArray());
                }
            }
        }
    }
}
