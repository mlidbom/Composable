using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Composable.Messaging.Events;

namespace Composable.Messaging.Buses
{
    ///<summary>Dispatches messages within a process.</summary>
    interface IInProcessServiceBus
    {
        void Publish(IEvent anEvent);
        TResult Get<TResult>(IQuery<TResult> query) where TResult : IQueryResult;
        void Send(ICommand message);
    }

    ///<summary>Dispatches messages between processes.</summary>
    interface IInterProcessServiceBus : IDisposable
    {
        void SendAtTime(DateTime sendAt, ICommand message);
        void Publish(IEvent anEvent);
        TResult Query<TResult>(IQuery<TResult> query) where TResult : IQueryResult;
        Task<TResult> QueryAsync<TResult>(IQuery<TResult> query) where TResult : IQueryResult;

        void Send(ICommand command);
        void Start();
        void Stop();
    }

    public interface IMessageSpy
    {
        IEnumerable<IMessage> DispatchedMessages { get; }
    }

    interface IMessageHandlerRegistry
    {
        Action<object> GetCommandHandler(ICommand message);

        Func<IQuery<TResult>, TResult> GetQueryHandler<TResult>(IQuery<TResult> query) where TResult : IQueryResult;

        IEventDispatcher<IEvent> CreateEventDispatcher();

        bool Handles(object aMessage);
    }

    public interface IMessageHandlerRegistrar
    {
        // ReSharper disable UnusedMethodReturnValue.Global
        IMessageHandlerRegistrar RegisterEventHandler<TEvent>(Action<TEvent> handler) where TEvent : IEvent;
        IMessageHandlerRegistrar RegisterCommandHandler<TCommand>(Action<TCommand> handler) where TCommand : ICommand;
        IMessageHandlerRegistrar RegisterQueryHandler<TQuery, TResult>(Func<TQuery, TResult> handler) where TQuery : IQuery<TResult> where TResult : IQueryResult;
        // ReSharper restore UnusedMethodReturnValue.Global
    }
}
