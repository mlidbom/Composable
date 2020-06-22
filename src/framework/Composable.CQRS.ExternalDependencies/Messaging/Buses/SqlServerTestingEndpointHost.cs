using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DependencyInjection;
using Composable.Messaging.Buses.Implementation;
using Composable.Refactoring.Naming;
using Composable.System.Linq;

namespace Composable.Messaging.Buses
{
    //Refactor: This must not be SQL Server dependent.
    class SqlServerTestingEndpointHost : SqlServerEndpointHost, ITestingEndpointHost, IEndpointRegistry
    {
        public SqlServerTestingEndpointHost(IRunMode mode, Func<IRunMode, IDependencyInjectionContainer> containerFactory) : base(mode, containerFactory)
        {
            GlobalBusStateTracker = new GlobalBusStateTracker();
        }

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
                builder.Container.RegisterSqlServerPersistenceLayer(builder.Configuration);
                Endpoints.Select(otherEndpoint => otherEndpoint.ServiceLocator.Resolve<TypeMapper>())
                         .ForEach(otherTypeMapper => ((TypeMapper)builder.TypeMapper).IncludeMappingsFrom(otherTypeMapper));
            });

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
                                                                        .Where(@this => !(@this.Address is null))
                                                                        .Select(@this => @this.Address!)
                                                                        .ToList();
    }
}
