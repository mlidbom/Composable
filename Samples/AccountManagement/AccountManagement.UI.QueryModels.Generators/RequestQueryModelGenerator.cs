using AccountManagement.Domain.Events;
using AccountManagement.Domain.Events.PropertyUpdated;
using Composable.CQRS.EventSourcing;

namespace AccountManagement.UI.QueryModels.Generators
{
    public class RequestQueryModelGenerator : SingleAggregateDocumentGenerator<RequestQueryModelGenerator,AccountQueryModel, IAccountEvent, IEventStoreReader>
    {
        public RequestQueryModelGenerator(IEventStoreReader session) : base(session)
        {
            RegisterHandlers()
                .For<IAccountEmailPropertyUpdatedEvent>(e => Model.Email = e.Email)
                .For<IAccountPasswordPropertyUpdateEvent>(e => Model.Password = e.Password);
        }
    }
}
