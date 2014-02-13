namespace Composable.CQRS.UI.Event
{
    public class UIViewModelStatus : UICommandResult
    {
        public string Type { get { return "viewmodelstatus"; } }
        public string ViewModelId { get; set; }
    }
}