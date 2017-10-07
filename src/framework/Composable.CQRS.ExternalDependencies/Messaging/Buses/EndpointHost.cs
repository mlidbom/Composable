using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.Messaging.Buses.Implementation;
using Composable.System.Collections.Collections;
using Composable.System.Linq;

namespace Composable.Messaging.Buses
{
    class EndpointHost : IEndpointHost
    {
        readonly IRunMode _mode;
        bool _disposed;
        protected readonly List<IEndpoint> Endpoints = new List<IEndpoint>();
        readonly IGlobalBusStrateTracker _globalBusStrateTracker = new GlobalBusStrateTracker();
        readonly Router _router = new Router();

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
            var builder = new EndpointBuilder(name, _mode, _globalBusStrateTracker, _router);

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
            _router.Connect(endpoint);

            var myRegistry = endpoint.ServiceLocator.Resolve<MessageHandlerRegistry>();
            var myCommandHandlers = myRegistry._commandHandlers.ToArray();
            var myCommandHandlersReturningResults = myRegistry._commandHandlersReturningResults.ToArray();
            var myQueryHandlers = myRegistry._queryHandlers.ToArray();
            var myEventRegistrations = myRegistry._eventHandlerRegistrations.ToArray();

            var registryWithAllAlreadyCrossConnectedHandlers = Endpoints.FirstOrDefault()?.ServiceLocator.Resolve<MessageHandlerRegistry>();
            if(registryWithAllAlreadyCrossConnectedHandlers != null)
            {
                registryWithAllAlreadyCrossConnectedHandlers._commandHandlers.ForEach(handler => myRegistry._commandHandlers.Add(handler.Key, handler.Value));
                registryWithAllAlreadyCrossConnectedHandlers._commandHandlersReturningResults.ForEach(handler => myRegistry._commandHandlersReturningResults.Add(handler.Key, handler.Value));
                registryWithAllAlreadyCrossConnectedHandlers._eventHandlerRegistrations.ForEach(registration => myRegistry._eventHandlerRegistrations.Add(registration));
                registryWithAllAlreadyCrossConnectedHandlers._queryHandlers.ForEach(handler => myRegistry._queryHandlers.Add(handler.Key, handler.Value));
            }

            foreach(var registryWithoutMyHandlers in Endpoints.Select(endpointWithoutMyHandlers => endpointWithoutMyHandlers.ServiceLocator.Resolve<MessageHandlerRegistry>()))
            {
                myCommandHandlers.ForEach(handler => registryWithoutMyHandlers._commandHandlers.Add(handler.Key, handler.Value));
                myCommandHandlersReturningResults.ForEach(handler => registryWithoutMyHandlers._commandHandlersReturningResults.Add(handler.Key, handler.Value));
                myEventRegistrations.ForEach(registration => registryWithoutMyHandlers._eventHandlerRegistrations.Add(registration));
                myQueryHandlers.ForEach(handler => registryWithoutMyHandlers._queryHandlers.Add(handler.Key, handler.Value));
            }
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

    class Router : IRouter
    {
        readonly Dictionary<Type, IList<IInbox>> _eventRoutes = new Dictionary<Type, IList<IInbox>>();
        readonly Dictionary<Type, IInbox> _commandRoutes = new Dictionary<Type, IInbox>();
        readonly Dictionary<Type, IInbox> _queryRoutes = new Dictionary<Type, IInbox>();

        public void Connect(IEndpoint endpoint)
        {
            IMessageHandlerRegistry messageHandlers = endpoint.ServiceLocator.Resolve<IMessageHandlerRegistry>();
            var inbox = endpoint.ServiceLocator.Resolve<IInbox>();
            foreach (var messageType in messageHandlers.HandledTypes())
            {
                if(IsEvent(messageType))
                {
                    Contract.AssertThat(!IsCommand(messageType), !IsQuery(messageType));
                    _eventRoutes.GetOrAdd(messageType, () => new List<IInbox>()).Add(inbox);
                }else if(typeof(ICommand).IsAssignableFrom(messageType))
                {
                    Contract.AssertThat(!IsEvent(messageType), !IsQuery(messageType), !_commandRoutes.ContainsKey(messageType));
                    _commandRoutes.Add(messageType, inbox);
                }
                else if(typeof(IQuery).IsAssignableFrom(messageType))
                {
                    Contract.AssertThat(!IsEvent(messageType), !IsCommand(messageType), !_queryRoutes.ContainsKey(messageType));
                    _queryRoutes.Add(messageType, inbox);
                }
            }
        }

        static bool IsCommand(Type type) => typeof(ICommand).IsAssignableFrom(type);
        static bool IsEvent(Type type) => typeof(IEvent).IsAssignableFrom(type);
        static bool IsQuery(Type type) => typeof(IQuery).IsAssignableFrom(type);

        public IInbox RouteFor(ICommand command) => _commandRoutes[command.GetType()];
        public IEnumerable<IInbox> RouteFor(IEvent @event) => _eventRoutes[@event.GetType()];
        public IInbox RouteFor(IQuery query) => _queryRoutes[query.GetType()];
    }


}
