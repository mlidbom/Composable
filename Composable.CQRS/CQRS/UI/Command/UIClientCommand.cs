namespace Composable.CQRS.UI.Command
{
    public class UIClientCommand
    {
        public string CommandName { get; set; }
        public string Redirect { get; set; }
        public string ControlId { get; set; }
        public bool WaitForStatus { get; set; }
    }
}
