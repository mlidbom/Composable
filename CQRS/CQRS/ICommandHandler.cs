namespace Composable.CQRS
{
    public interface ICommandHandler<in TCommand> where TCommand : IDomainCommand<TCommand>
    {
        void Execute(TCommand command);
    }
}