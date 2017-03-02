using System;
using Composable.CQRS.EventSourcing;

namespace Composable.Messaging.Buses
{
    public interface IMessageHandlerRegistrar
    {
        IMessageHandlerRegistrar ForEvent<TEvent>(Action<TEvent> handler) where TEvent : IEvent;
        IMessageHandlerRegistrar ForCommand<TCommand>(Action<TCommand> handler) where TCommand : ICommand;
        IMessageHandlerRegistrar ForQuery<TQuery, TResult>(Func<TQuery, TResult> handler) where TQuery : IQuery<TResult> where TResult : IQueryResult;
    }
}
