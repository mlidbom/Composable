namespace Composable.CQRS.EventHandling
{
    public interface IMutableEventDispatcher<in TEvent> : IEventDispatcher<TEvent>
    {
        IEventHandlerRegistrar<TEvent> Register();
    }
}