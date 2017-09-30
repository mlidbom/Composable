using System;
using System.Linq;
using Composable.DependencyInjection;

namespace Composable.Messaging.Buses
{
    class TestingEndpointHost : EndpointHost, ITestingEndpointHost
    {
        readonly IEndpoint _clientEndpoint;
        public TestingEndpointHost(IRunMode mode) : base(mode)
        {
            _clientEndpoint = RegisterAndStartEndpoint($"{nameof(TestingEndpointHost)}_Default_Client_Endpoint", _ => { });
        }


        public void WaitForEndpointsToBeAtRest(TimeSpan? timeoutOverride) { Endpoints.ForEach(endpoint => endpoint.AwaitNoMessagesInFlight(timeoutOverride)); }

        public IServiceBus ClientBus => _clientEndpoint.ServiceLocator.Resolve<IServiceBus>();
        public IApiNavigator ClientNavigator => new ApiNavigator(ClientBus);

        protected override void InternalDispose()
        {
            WaitForEndpointsToBeAtRest(null);

            var exceptions = Endpoints
                .SelectMany(endpoint => endpoint.ServiceLocator
                                                .Resolve<ServiceBus>().ThrownExceptions)
                .ToList();

            base.InternalDispose();


            if(exceptions.Any())
            {
                throw new AggregateException("Unhandled exceptions thrown in bus", exceptions.ToArray());
            }
        }
    }
}
