namespace Composable.CQRS
{
    public interface ICommandService
    {
        void Execute<TCommand>(TCommand command) where TCommand : IDomainCommand;

        void Execute<TCommand, TEntityId>(TCommand command)
            where TCommand : IEntityCommand<TEntityId>;
    }
}