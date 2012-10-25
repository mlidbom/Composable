using JetBrains.Annotations;

namespace Composable.CQRS
{
    [UsedImplicitly]
    public interface IEntityCommandHandler<in TCommand>
    {
        void Execute(TCommand command);
    }
}