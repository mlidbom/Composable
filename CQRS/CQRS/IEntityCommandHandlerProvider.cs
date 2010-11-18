namespace Composable.CQRS
{
    public interface IEntityCommandHandlerProvider
    {
        ICommandHandler<TCommand> Provide<TCommand>(TCommand command);
    }
}