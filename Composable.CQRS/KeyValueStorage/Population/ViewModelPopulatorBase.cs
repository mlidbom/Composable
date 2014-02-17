using System;
using System.Collections.Generic;
using System.Linq;
using Composable.CQRS.EventSourcing;
using Composable.ServiceBus;

namespace Composable.KeyValueStorage.Population
{
    public abstract class ViewModelPopulatorBase : IViewModelPopulator
    {
        protected IServiceBus _bus;
        protected IEventStore _events;

        protected ViewModelPopulatorBase(IServiceBus bus, IEventStore events)
        {
            _bus = bus;
            _events = events;
        }

        public void Populate(Guid entityId)
        {
            var aggregateRootEvents = _events.GetAggregateHistory(entityId).ToList();
            
            InitializeRepopulation(aggregateRootEvents);

            foreach (var aggregateRootEvent in aggregateRootEvents)
            {
                _bus.Publish(aggregateRootEvent);
            }            
        }

        protected abstract void InitializeRepopulation(List<IAggregateRootEvent> aggregateRootEvents);
    }
}