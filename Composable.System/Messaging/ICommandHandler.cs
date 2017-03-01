namespace Composable.Messaging
{
  using JetBrains.Annotations;

  [UsedImplicitly] public interface ICommandHandler<in TCommand>
  {
    void Execute(TCommand command);
  }
}
