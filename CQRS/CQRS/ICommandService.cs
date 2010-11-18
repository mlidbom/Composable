namespace Composable.CQRS
{
    public interface ICommandService
    {
        void Execute<TCommand>(TCommand command) where TCommand : IDomainCommand;

        void Execute<TCommand, TEntity>(TCommand command)
            where TCommand : IEntityCommand<TEntity>
            where TEntity : ICommandHandler<TCommand>;
    }
}