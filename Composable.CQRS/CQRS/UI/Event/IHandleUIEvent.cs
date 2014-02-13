namespace Composable.CQRS.UI.Event
{
    public interface IHandleUIEvent<in TEvent> where TEvent : IUIEvent
    {
        void Handle(TEvent evt);
    }
}