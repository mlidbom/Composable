namespace Composable.CQRS.UI.Command
{
    public interface IUIValidationError
    {
        string Property { get; }
        string Error { get; }
    }
}