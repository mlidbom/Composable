namespace Composable.CQRS.UI.Event
{
    public class UICommandStart : UICommandResult
    {
        public string Type { get { return "commandstart"; } }
    }
}