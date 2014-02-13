using System.Collections.Generic;

namespace Composable.CQRS.UI.Command
{
    public interface IUICommand
    {
        global::System.Guid Id { get; set; }
        bool IsValid { get; }
        IEnumerable<IUIValidationError> ValidationErrors { get; }
    }
}