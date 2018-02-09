using System;
using System.Collections.Generic;
using System.Linq;
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
            public static ITestingEndpointHost CreateWithClientEndpoint(Func<IRunMode, IDependencyInjectionContainer> containerFactory, TestingMode mode = TestingMode.DatabasePool) => new TestingEndpointHost(new RunMode(isTesting: true, testingMode: mode), containerFactory, createClientEndpoint: true);

            public static ITestingEndpointHost Create(Func<IRunMode, IDependencyInjectionContainer> containerFactory, TestingMode mode = TestingMode.DatabasePool) => new TestingEndpointHost(new RunMode(isTesting: true, testingMode: mode), containerFactory, createClientEndpoint: false);
        }

        bool _isStarted;
        public void Start()
        {
            Assert.State.Assert(!_isStarted, Endpoints.None(endpoint => endpoint.IsRunning));
            _isStarted = true;

            var startedEndpoints = new List<IEndpoint>();
            Endpoints.Where(endpoint => !endpoint.IsRunning).ForEach(endpointToStart =>
            {
                endpointToStart.Start();

                var endpointTransport = endpointToStart.ServiceLocator.Resolve<IInterprocessTransport>();

                //Any existing endpoint contains all the types since it is merged with any and all other existing endpoints.
                startedEndpoints.FirstOrDefault()?.ServiceLocator.Resolve<TypeMapper>().MergeMappingsWith(endpointToStart.ServiceLocator.Resolve<TypeMapper>());

                startedEndpoints.ForEach(existingEndpoint =>
                {
                    existingEndpoint.ServiceLocator.Resolve<IInterprocessTransport>().Connect(endpointToStart);
                    endpointTransport.Connect(existingEndpoint);
                });

                endpointTransport.Connect(endpointToStart); //Yes connect it to itself so that it can send messages to itself :)

                startedEndpoints.Add(endpointToStart);
            });
        }

        public IEndpoint RegisterEndpoint(string name, EndpointId id, Action<IEndpointBuilder> setup)
        {
            var builder = new EndpointBuilder(GlobalBusStateTracker, _containerFactory(_mode), name, id);

            setup(builder);

            var endpoint = builder.Build();

            Endpoints.Add(endpoint);
            return endpoint;
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
