using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DependencyInjection;

namespace Composable.Messaging.Buses
{
    class TestingEndpointHost : EndpointHost, ITestingEndpointHost
    {
        readonly IEndpoint _clientEndpoint;
        public TestingEndpointHost(IRunMode mode, Func<IRunMode, IDependencyInjectionContainer> containerFactory, bool createClientEndpoint = true) : base(mode, containerFactory)
        {
            if(createClientEndpoint)
            {
                _clientEndpoint = RegisterEndpoint($"{nameof(TestingEndpointHost)}_Default_Client_Endpoint", new EndpointId(Guid.Parse("D4C869D2-68EF-469C-A5D6-37FCF2EC152A")), _ => {});
            }
        }


        public void WaitForEndpointsToBeAtRest(TimeSpan? timeoutOverride = null) { Endpoints.ForEach(endpoint => endpoint.AwaitNoMessagesInFlight(timeoutOverride)); }


        public IEndpoint RegisterTestingEndpoint(string name = null, EndpointId id = null, Action<IEndpointBuilder> setup = null)
        {
            var endpointId  = id ?? new EndpointId(Guid.NewGuid());
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

        public IEndpoint ClientEndpoint => _clientEndpoint;

        public IServiceBusSession ClientBusSession => _clientEndpoint.ServiceLocator.Resolve<IServiceBusSession>();

        public IRemoteApiNavigatorSession RemoteNavigator => _clientEndpoint.ServiceLocator.Resolve<IRemoteApiNavigatorSession>();

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
    }
}
