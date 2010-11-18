namespace Composable.CQRS
{
    public interface IEntityCommandHandlerProvider
    {
        ICommandHandler<TCommand> Provide<TCommand, TEntity>(TCommand command) 
            where TEntity : ICommandHandler<TCommand>
            where TCommand : IEntityCommand<TEntity>;
    }
}