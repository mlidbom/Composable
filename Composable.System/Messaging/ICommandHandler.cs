using JetBrains.Annotations;

namespace Composable.Messaging
{
    [UsedImplicitly] public interface ICommandHandler<in TCommand>
    {
        void Execute(TCommand command);
    }
}
