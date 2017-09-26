using System;
using System.Linq;
using Composable.DependencyInjection;

namespace Composable.Messaging.Buses
{
    class TestingEndpointHost : EndpointHost, ITestingEndpointHost
    {
        public TestingEndpointHost(IRunMode mode) : base(mode) {}
        public void WaitForEndpointsToBeAtRest() { Endpoints.ForEach(endpoint => endpoint.AwaitNoMessagesInFlight()); }

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
