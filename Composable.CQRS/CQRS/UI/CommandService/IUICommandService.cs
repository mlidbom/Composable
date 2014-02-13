using Composable.CQRS.UI.Command;

namespace Composable.CQRS.UI.CommandService
{
    public interface IUICommandService
    {
        void HandleCommand<TCommand>(TCommand command) where TCommand : IUICommand;
    }
}