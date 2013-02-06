namespace Composable.CQRS.UI.Command
{
    public interface IHandleUICommand<in TCommand> where TCommand : IUICommand
    {
        void Handle(TCommand command);
    }
}