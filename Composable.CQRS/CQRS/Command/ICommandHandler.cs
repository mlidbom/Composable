using JetBrains.Annotations;

namespace Composable.CQRS
{
    [UsedImplicitly]
    public interface ICommandHandler<in TCommand>
    {
        void Execute(TCommand command);
    }
}