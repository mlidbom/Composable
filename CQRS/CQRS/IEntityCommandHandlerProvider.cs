namespace Composable.CQRS
{
    public interface IEntityCommandHandlerProvider
    {
        ICommandHandler<TCommand> Provide<TCommand, TEntityId>(TCommand command) where TCommand : IEntityCommand<TEntityId>;
    }
}