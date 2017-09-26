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


        public void WaitForEndpointsToBeAtRest() { Endpoints.ForEach(endpoint => endpoint.AwaitNoMessagesInFlight()); }

        public IServiceBus ClientBus => _clientEndpoint.ServiceLocator.Resolve<IServiceBus>();

        protected override void InternalDispose()
        {
            WaitForEndpointsToBeAtRest();

            var exceptions = Endpoints
                .SelectMany(endpoint => endpoint.ServiceLocator
                                                .Resolve<ServiceBus>().ThrownExceptions)
                .ToList();

            Endpoints.ForEach(endpoint => endpoint.Dispose());

            base.InternalDispose();

            if(exceptions.Any())
            {
                throw new AggregateException("Unhandled exceptions thrown in bus", exceptions.ToArray());
            }
        }
    }
}
