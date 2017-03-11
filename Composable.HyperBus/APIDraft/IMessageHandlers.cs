// ReSharper disable All
namespace Composable.HyperBus.APIDraft
{
    public interface ICommandHandler<in TCommand, out TReturnValue> where TCommand : ICommand<TReturnValue>
    {
        TReturnValue Handle(TCommand command);
    }

    public interface ICommandHandler<in TCommand> where TCommand : ICommand
    {
        void Handle(TCommand command);
    }

    public interface IQueryHandler<in TQuery, out TReturnValue> where TQuery : IQuery<TReturnValue>
    {
        TReturnValue Handle(TQuery command);
    }

    public interface IEventHandler<in TEvent> where TEvent : IEvent
    {
        void Handle(TEvent @event);
    }
}