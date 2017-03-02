using System;
using Composable.CQRS.EventSourcing;
using Composable.Messaging.Events;

namespace Composable.Messaging.Buses
{
    public interface IMessageHandlerRegistry
    {
        Action<object> GetCommandHandler(ICommand message);

        Func<IQuery<TResult>, TResult> GetQueryHandler<TResult>(IQuery<TResult> query) where TResult : IQueryResult;

        IEventDispatcher<IEvent> CreateEventDispatcher();

        bool Handles(object aMessage);
    }
}
