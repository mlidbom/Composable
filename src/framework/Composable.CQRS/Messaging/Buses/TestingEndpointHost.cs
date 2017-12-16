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

        public TException AssertThrown<TException>() where TException : Exception
        {
            WaitForEndpointsToBeAtRest();
            var matchingException = GetThrownExceptions().OfType<TException>().SingleOrDefault();
            if(matchingException == null)
            {
                throw new Exception("Matching exception not thrown.");
            }
            _handledExceptions.Add(matchingException);
            return matchingException;
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
