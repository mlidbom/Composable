using System;

namespace Composable.HyperBus.APIDraft
{
    public interface IMessageHandlerRegistrar
    {
        IMessageHandlerRegistrar Query<TQuery, TReturnValue>(Func<TQuery, TReturnValue> handler) where TQuery : IQuery<TReturnValue>;
        IMessageHandlerRegistrar Command<TCommand, TReturnValue>(Func<TCommand, TReturnValue> handler) where TCommand : ICommand<TReturnValue>;
        IMessageHandlerRegistrar Command<TCommand>(Action<TCommand> handler) where TCommand : ICommand;
        IMessageHandlerRegistrar Event<TEvent>(Action<TEvent> handler) where TEvent : IEvent;

        void FromAssemblyContaining(Type type);
    }
}