namespace Composable.CQRS.UI.Command
{
    public class CommandExecutionError : IUIValidationError
    {
        public CommandExecutionError(string error)
        {
            Error = error;
            Property = "";
        }

        public string Property { get; private set; }
        public string Error { get; private set; }
    }
}