using System;
using Composable.CQRS.EventSourcing;
using Composable.Messaging.Events;

namespace Composable.Messaging.Buses
{
    public interface IMessageHandlerRegistry
    {
        Action<object> GetHandlerFor(ICommand message);

        IEventDispatcher<IEvent> CreateEventDispatcher();

        bool Handles(object aMessage);
    }
}
