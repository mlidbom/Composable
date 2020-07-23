using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.Logging;
using Composable.Messaging.Buses.Implementation;
using Composable.SystemCE.Linq;
using Composable.SystemCE.Reflection.Threading;

namespace Composable.Messaging.Buses
{
    public class EndpointHost : IEndpointHost
    {
        readonly IRunMode _mode;
        readonly Func<IRunMode, IDependencyInjectionContainer> _containerFactory;
        bool _disposed;
        protected List<IEndpoint> Endpoints { get; } = new List<IEndpoint>();
        internal IGlobalBusStateTracker GlobalBusStateTracker;

        readonly ILogger _log = Logger.For<EndpointHost>();

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

        public IEndpoint RegisterEndpoint(string name, EndpointId id, Action<IEndpointBuilder> setup) => InternalRegisterEndpoint(new EndpointConfiguration(name, id, _mode, isPureClientEndpoint: false), setup);

        IEndpoint InternalRegisterEndpoint(EndpointConfiguration configuration, Action<IEndpointBuilder> setup)
        {
            using var builder = new ServerEndpointBuilder(this, GlobalBusStateTracker, _containerFactory(_mode), configuration);
            setup(builder);

            var endpoint = builder.Build();

            Endpoints.Add(endpoint);
            return endpoint;
        }

        static readonly EndpointConfiguration ClientEndpointConfiguration = new EndpointConfiguration(name: $"{nameof(EndpointHost)}_Default_Client_Endpoint",
                                                                                                      id: new EndpointId(Guid.Parse("D4C869D2-68EF-469C-A5D6-37FCF2EC152A")),
                                                                                                      mode: RunMode.Production,
                                                                                                      isPureClientEndpoint: true);

        public IEndpoint RegisterClientEndpoint(Action<IEndpointBuilder> setup) => InternalRegisterEndpoint(ClientEndpointConfiguration, setup);

        bool _isStarted;

        public async Task StartAsync() => await _log.ExceptionsAndRethrowAsync(async () =>
        {
            Assert.State.Assert(!_isStarted, Endpoints.None(endpoint => endpoint.IsRunning));
            _isStarted = true;

            await Task.WhenAll(Endpoints.Select(endpointToStart => endpointToStart.InitAsync())).NoMarshalling();
            await Task.WhenAll(Endpoints.Select(endpointToStart => endpointToStart.ConnectAsync())).NoMarshalling();
        }).NoMarshalling();

        public void Start() => StartAsync().WaitUnwrappingException();

        public void Stop() => _log.ExceptionsAndRethrow(() =>
        {
            Assert.State.Assert(_isStarted);
            _isStarted = false;
            Endpoints.Where(endpoint => endpoint.IsRunning).ForEach(endpoint => endpoint.Stop());
        });

        protected virtual void Dispose(bool disposing) => _log.ExceptionsAndRethrow(() =>
        {
            if(!_disposed)
            {
                _disposed = true;
                if(_isStarted)
                {
                    Stop();
                }

                Endpoints.ForEach(endpoint => endpoint.Dispose());
            }
        });

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
         }
    }
}
