#region usings

using System.Transactions;
using Castle.Windsor;
using Composable.CQRS;
using Composable.DomainEvents;
using Microsoft.Practices.ServiceLocation;

#endregion

namespace Composable.StuffThatDoesNotBelongHere
{
    public class EventPersistenceService : IEventPersistenceService
    {
        private readonly IWindsorContainer _serviceLocator;

        public EventPersistenceService(IWindsorContainer serviceLocator)
        {
            _serviceLocator = serviceLocator;
        }

        public virtual void Persist<TEvent>(TEvent evt) where TEvent : IDomainEvent
        {
            using(var transaction = new TransactionScope())
            {
                var handler = _serviceLocator.Resolve<IEventPersister<TEvent>>();
                handler.Persist(evt);
                transaction.Complete();
            }
        }
    }
}