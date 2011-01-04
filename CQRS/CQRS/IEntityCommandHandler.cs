namespace Composable.CQRS
{
    public interface IEntityCommandHandler<in TCommand>
    {
        void Execute(TCommand command);
    }
}