using System.Transactions;
using Composable.CQRS;
using Composable.DomainEvents;
using Manpower.System.Web.Mvc.StuffThatDoesNotBelongHere;
using Microsoft.Practices.ServiceLocation;

namespace Composable.StuffThatDoesNotBelongHere
{
    public class EventPersistenceService : IEventPersistenceService
    {
        private readonly IServiceLocator _serviceLocator;

        public EventPersistenceService(IServiceLocator serviceLocator)
        {
            _serviceLocator = serviceLocator;
        }

        public virtual void Persist<TEvent>(TEvent evt) where TEvent : IDomainEvent
        {
            using (DomainEvent.Register<IDomainEvent>(x => {}))
            {
                using (var transaction = new TransactionScope())
                {
                    var handler = _serviceLocator.GetSingleInstance<IEventPersister<TEvent>>();
                    handler.Persist(evt);
                    transaction.Complete();
                }
            }
        }
    }
}