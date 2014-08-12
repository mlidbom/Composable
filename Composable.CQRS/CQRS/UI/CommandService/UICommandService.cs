using Castle.Windsor;
using Composable.CQRS.UI.Command;
using Composable.CQRS.Windsor;

namespace Composable.CQRS.UI.CommandService
{
    public class UICommandService : IUICommandService
    {
        private readonly IWindsorContainer _container;

        public UICommandService(IWindsorContainer container)
        {
            _container = container;
        }

        public void HandleCommand<TCommand>(TCommand command) where TCommand : IUICommand
        {
            _container.UseComponent<IHandleUICommand<TCommand>>(handler => handler.Handle(command));
        }
    }
}