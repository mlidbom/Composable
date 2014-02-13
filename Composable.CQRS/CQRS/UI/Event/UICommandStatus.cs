using System.Collections.Generic;
using Composable.CQRS.UI.Command;

namespace Composable.CQRS.UI.Event
{
    public class UICommandStatus : UICommandResult
    {
        public string Type { get { return "commandstatus"; } }
        public List<IUIValidationError> ValidationErrors { get; set; }
    }
}