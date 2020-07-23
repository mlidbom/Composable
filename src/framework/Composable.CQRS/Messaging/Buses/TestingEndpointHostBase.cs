using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Composable.DependencyInjection;
using Composable.Logging;
using Composable.Messaging.Buses.Implementation;
using Composable.Refactoring.Naming;
using Composable.SystemCE.LinqCE;

namespace Composable.Messaging.Buses
{
    public class TestingEndpointHostBase : EndpointHost, ITestingEndpointHost, IEndpointRegistry
    {
        readonly ILogger _log = Logger.For<TestingEndpointHostBase>();

        readonly List<Exception> _expectedExceptions = new List<Exception>();
        public TestingEndpointHostBase(IRunMode mode, Func<IRunMode, IDependencyInjectionContainer> containerFactory) : base(mode, containerFactory)
        {
            GlobalBusStateTracker = new GlobalBusStateTracker();
        }

        public IEnumerable<EndPointAddress> ServerEndpoints => Endpoints.Where(endpoint => endpoint.ServiceLocator.Resolve<IMessageHandlerRegistry>().HandledRemoteMessageTypeIds().Any())
                                                                        .Where(@this => !(@this.Address is null))
                                                                        .Select(@this => @this.Address!)
                                                                        .ToList();

        public void WaitForEndpointsToBeAtRest(TimeSpan? timeoutOverride = null) { Endpoints.ForEach(endpoint => endpoint.AwaitNoMessagesInFlight(timeoutOverride)); }
        public IEndpoint RegisterTestingEndpoint(string? name = null, EndpointId? id = null, Action<IEndpointBuilder>? setup = null)
        {
            var endpointId = id ?? new EndpointId(Guid.NewGuid());
            name ??= $"TestingEndpoint-{endpointId.GuidValue}";
            setup ??= (builder => {});
            return RegisterEndpoint(name, endpointId, setup);
        }

        public IEndpoint RegisterClientEndpointForRegisteredEndpoints() =>
            RegisterClientEndpoint(builder =>
            {
                ExtraEndpointConfiguration(builder);
                Endpoints.Select(otherEndpoint => otherEndpoint.ServiceLocator.Resolve<TypeMapper>())
                         .ForEach(otherTypeMapper => ((TypeMapper)builder.TypeMapper).IncludeMappingsFrom(otherTypeMapper));
            });

        internal virtual void ExtraEndpointConfiguration(IEndpointBuilder builder){}

        public TException AssertThrown<TException>() where TException : Exception
        {
            WaitForEndpointsToBeAtRest();
            var matchingException = GetThrownExceptions().OfType<TException>().SingleOrDefault();
            if(matchingException == null)
            {
                throw new Exception("Matching exception not thrown.");
            }

            _expectedExceptions.Add(matchingException);
            return matchingException;
        }

        bool _disposed;
        protected override void Dispose(bool disposing) => _log.ExceptionsAndRethrow(() =>
        {
            if(!_disposed)
            {
                _disposed = true;
                WaitForEndpointsToBeAtRest();

                var unHandledExceptions = GetThrownExceptions().Except(_expectedExceptions).ToList();

                base.Dispose(disposing);

                if(unHandledExceptions.Any())
                {
                    throw new AggregateException("Unhandled exceptions thrown in bus", unHandledExceptions.ToArray());
                }
            }
        });

        List<Exception> GetThrownExceptions() => GlobalBusStateTracker.GetExceptions().ToList();
    }
}
