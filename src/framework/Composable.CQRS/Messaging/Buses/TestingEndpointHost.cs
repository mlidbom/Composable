using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DependencyInjection;
using Composable.Messaging.Buses.Implementation;

namespace Composable.Messaging.Buses
{
    class TestingEndpointHost : EndpointHost, ITestingEndpointHost, IEndpointRegistry
    {
        public TestingEndpointHost(IRunMode mode, Func<IRunMode, IDependencyInjectionContainer> containerFactory) : base(mode, containerFactory)
        {

        }

        public void WaitForEndpointsToBeAtRest(TimeSpan? timeoutOverride = null) { Endpoints.ForEach(endpoint => endpoint.AwaitNoMessagesInFlight(timeoutOverride)); }

        public IEndpoint RegisterTestingEndpoint(string name = null, EndpointId id = null, Action<IEndpointBuilder> setup = null)
        {
            var endpointId = id ?? new EndpointId(Guid.NewGuid());
            name = name ?? $"TestingEndpoint-{endpointId.GuidValue}";
            setup = setup ?? (builder => {});
            return RegisterEndpoint(name, endpointId, setup);
        }

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

        List<Exception> GetThrownExceptions() => GlobalBusStateTracker.GetExceptions().ToList();

        public IEnumerable<EndPointAddress> ServerEndpoints => Endpoints.Where(endpoint => endpoint.ServiceLocator.Resolve<IMessageHandlerRegistry>().HandledRemoteMessageTypeIds().Any())
                                                                        .Select(@this => @this.Address)
                                                                        .ToList();
    }
}
