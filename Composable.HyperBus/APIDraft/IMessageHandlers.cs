using JetBrains.Annotations;

namespace Composable.HyperBus.APIDraft
{
    [UsedImplicitly]
    public interface ICommandHandler<in TCommand, out TReturnValue> where TCommand : ICommand<TReturnValue>
    {
        TReturnValue Handle(TCommand command);
    }

    [UsedImplicitly]
    public interface ICommandHandler<in TCommand> where TCommand : ICommand
    {
        void Handle(TCommand command);
    }

    [UsedImplicitly]
    public interface IQueryHandler<in TQuery, out TReturnValue> where TQuery : IQuery<TReturnValue>
    {
        TReturnValue Handle(TQuery command);
    }

    [UsedImplicitly]
    public interface IEventHandler<in TEvent> where TEvent : IEvent
    {
        void Handle(TEvent @event);
    }
}