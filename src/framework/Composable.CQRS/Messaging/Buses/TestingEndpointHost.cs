using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DependencyInjection;
using Composable.Messaging.Buses.Implementation;
using Composable.System.Linq;

namespace Composable.Messaging.Buses
{
    class TestingEndpointHost : EndpointHost, ITestingEndpointHost
    {
        readonly IEndpoint _clientEndpoint;
        public TestingEndpointHost(IRunMode mode, Func<IRunMode, IDependencyInjectionContainer> containerFactory) : base(mode, containerFactory)
        {
            _clientEndpoint = RegisterAndStartEndpoint($"{nameof(TestingEndpointHost)}_Default_Client_Endpoint", _ => { });
        }


        public void WaitForEndpointsToBeAtRest(TimeSpan? timeoutOverride = null) { Endpoints.ForEach(endpoint => endpoint.AwaitNoMessagesInFlight(timeoutOverride)); }

        public void AssertThrown<TException>(Func<TException, bool> condition = null) where TException : Exception
        {
            WaitForEndpointsToBeAtRest();
            condition = condition ?? (exception => true);
            var matchingExceptions = GetThrownExceptions().OfType<TException>().Where(condition).ToList();
            if(matchingExceptions.None())
            {
                throw new Exception("Matching exception not thrown.");
            }
            _handledExceptions.AddRange(matchingExceptions);
        }

        public IServiceBus ClientBus => _clientEndpoint.ServiceLocator.Resolve<IServiceBus>();
        public IApiNavigator ClientNavigator => new ApiNavigator(ClientBus);


        protected override void InternalDispose()
        {
            WaitForEndpointsToBeAtRest();

            var unHandledExceptions = GetThrownExceptions().Except(_handledExceptions);

            base.InternalDispose();


            if(unHandledExceptions.Any())
            {
                throw new AggregateException("Unhandled exceptions thrown in bus", unHandledExceptions.ToArray());
            }
        }

        List<Exception> _handledExceptions = new List<Exception>();

        List<Exception> GetThrownExceptions()
        {
            return Endpoints
                .SelectMany(endpoint => endpoint.ServiceLocator
                                                .Resolve<Inbox>().ThrownExceptions)
                .ToList();
        }
    }
}
