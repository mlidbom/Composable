namespace Composable.Messaging.Events
{
    public interface IMutableEventDispatcher<in TEvent> : IEventDispatcher<TEvent>
        where TEvent : class
    {
        IEventHandlerRegistrar<TEvent> Register();
    }
}