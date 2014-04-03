using AccountManagement.Domain.Events;
using AccountManagement.Domain.Events.EventStore.Services;
using AccountManagement.Domain.Events.PropertyUpdated;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.Query.Models.Generators;

namespace AccountManagement.UI.QueryModels.Generators
{
    /// <summary>Ad hoc creates a query model by reading and applying the events from the event store reader</summary>
    internal class RequestQueryModelGenerator :
        SingleAggregateQueryModelGenerator<RequestQueryModelGenerator, AccountQueryModel, IAccountEvent, IEventStoreReader>,
        IAccountManagementQueryModelGenerator
    {
        //Note the use of a custom interface. This lets us keep query model generators for different systems apart in the wiring.
        public RequestQueryModelGenerator(IAccountManagementEventStoreReader session) : base(session)
        {
            RegisterHandlers()
                .For<IAccountEmailPropertyUpdatedEvent>(e => Model.Email = e.Email)
                .For<IAccountPasswordPropertyUpdateEvent>(e => Model.Password = e.Password);
        }
    }
}
