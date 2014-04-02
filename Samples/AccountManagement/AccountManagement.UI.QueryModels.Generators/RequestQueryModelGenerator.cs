using AccountManagement.Domain.Events;
using AccountManagement.Domain.Events.PropertyUpdated;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.Query.Models.Generators;

namespace AccountManagement.UI.QueryModels.Generators
{
    public class RequestQueryModelGenerator : SingleAggregateQueryModelGenerator<RequestQueryModelGenerator,AccountQueryModel, IAccountEvent, IEventStoreReader>
    {
        public RequestQueryModelGenerator(IEventStoreReader session) : base(session)
        {
            RegisterHandlers()
                .For<IAccountEmailPropertyUpdatedEvent>(e => Model.Email = e.Email)
                .For<IAccountPasswordPropertyUpdateEvent>(e => Model.Password = e.Password);
        }
    }
}
