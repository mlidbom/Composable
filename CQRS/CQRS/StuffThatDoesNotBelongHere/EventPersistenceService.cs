using System.Transactions;
using Composable.CQRS;
using Composable.DomainEvents;
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
#pragma warning disable 612,618
            using (DomainEvent.RegisterShortTermSynchronousListener<IDomainEvent>(x => {}))
#pragma warning restore 612,618
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