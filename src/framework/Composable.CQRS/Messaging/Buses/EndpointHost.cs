using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DependencyInjection;
using Composable.Messaging.Buses.Implementation;
using Composable.Refactoring.Naming;

namespace Composable.Messaging.Buses
{
    public class EndpointHost : IEndpointHost
    {
        readonly IRunMode _mode;
        readonly Func<IRunMode, IDependencyInjectionContainer> _containerFactory;
        bool _disposed;
        protected readonly List<IEndpoint> Endpoints = new List<IEndpoint>();
        readonly IGlobalBusStateTracker _globalBusStateTracker = new GlobalBusStateTracker();

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
            public static ITestingEndpointHost BuildHost(Func<IRunMode, IDependencyInjectionContainer> containerFactory,
                                                         Action<ITestingEndpointHost> build,
                                                         TestingMode mode = TestingMode.DatabasePool)
            {
                var testingEndpointHost = new TestingEndpointHost(new RunMode(isTesting: true, testingMode: mode), containerFactory);
                build(testingEndpointHost);
                return testingEndpointHost;
            }
            public static ITestingEndpointHost CreateHost(Func<IRunMode, IDependencyInjectionContainer> containerFactory, TestingMode mode = TestingMode.DatabasePool) =>
                new TestingEndpointHost(new RunMode(isTesting: true, testingMode: mode), containerFactory);
        }

        public IEndpoint RegisterAndStartEndpoint(string name, EndpointId id, Action<IEndpointBuilder> setup)
        {
            var builder = new EndpointBuilder(_globalBusStateTracker, _containerFactory(_mode), name, id);

            setup(builder);

            var endpoint = builder.Build();

            var existingEndpoints = Endpoints.ToList();

            Endpoints.Add(endpoint);

            endpoint.Start();
            var endpointTransport = endpoint.ServiceLocator.Resolve<IInterprocessTransport>();

            //Any existing endpoint contains all the types since it is merged with any and all other existing endpoints.
            existingEndpoints.FirstOrDefault()?.ServiceLocator.Resolve<TypeMapper>().MergeMappingsWith(endpoint.ServiceLocator.Resolve<TypeMapper>());

            existingEndpoints.ForEach(existingEndpoint =>
            {
                existingEndpoint.ServiceLocator.Resolve<IInterprocessTransport>().Connect(endpoint);
                endpointTransport.Connect(existingEndpoint);
            });

            endpointTransport.Connect(endpoint); //Yes connect it to itself so that it can send messages to itself :)

            return endpoint;
        }

        public void Stop() { Endpoints.ForEach(endpoint => endpoint.Stop()); }

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
