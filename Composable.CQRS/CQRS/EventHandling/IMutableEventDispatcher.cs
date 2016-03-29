namespace Composable.CQRS.EventHandling
{
    public interface IMutableEventDispatcher<in TEvent> : IEventDispatcher<TEvent>
        where TEvent : class
    {
        IEventHandlerRegistrar<TEvent> Register();
    }
}