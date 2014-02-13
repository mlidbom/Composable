using Castle.Windsor;
using Composable.CQRS.UI.Command;

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
            var handler = _container.Resolve<IHandleUICommand<TCommand>>();
            handler.Handle(command);
        }
    }
}