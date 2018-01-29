using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DependencyInjection;

namespace Composable.Messaging.Buses
{
    class TestingEndpointHost : EndpointHost, ITestingEndpointHost
    {
        readonly IEndpoint _clientEndpoint;
        public TestingEndpointHost(IRunMode mode, Func<IRunMode, IDependencyInjectionContainer> containerFactory) : base(mode, containerFactory)
        {
            _clientEndpoint = RegisterAndStartEndpoint($"{nameof(TestingEndpointHost)}_Default_Client_Endpoint", new EndpointId(Guid.Parse("D4C869D2-68EF-469C-A5D6-37FCF2EC152A")), _ => { });
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

        public IEndpoint ClientEndpoint => _clientEndpoint;

        public ITransactionalMessageHandlerServiceBusSession ClientBusSession => _clientEndpoint.ServiceLocator.Resolve<ITransactionalMessageHandlerServiceBusSession>();

        public IRemoteApiBrowserSession RemoteBrowser => _clientEndpoint.ServiceLocator.Resolve<IRemoteApiBrowserSession>();

        protected override void InternalDispose()
        {
            WaitForEndpointsToBeAtRest();

            var unHandledExceptions = GetThrownExceptions().Except(_handledExceptions).ToList();

            base.InternalDispose();


            if(unHandledExceptions.Any())
            {
                throw new AggregateException("Unhandled exceptions thrown in bus", unHandledExceptions.ToArray());
            }
        }

        readonly List<Exception> _handledExceptions = new List<Exception>();

        List<Exception> GetThrownExceptions() => Endpoints.First().ServiceLocator.Resolve<IGlobalBusStateTracker>().GetExceptions().ToList();
    }
}
