using System;

namespace Composable.CQRS.UI.Event
{
    public class UICommandResult
    {
        public bool Success { get; set; }
        public bool WaitForStatus { get; set; }
        public string ControlId { get; set; }
        public string Redirect { get; set; }
        public Guid commandId { get; set; }
    }
}