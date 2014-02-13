namespace Composable.CQRS.UI.Command
{
    public class PropertyValidationError :IUIValidationError
    {
        public PropertyValidationError(string property, string error)
        {
            Property = property;
            Error = error;
        }

        public string Property { get; private set; }
        public string Error { get; private set; }
    }
}