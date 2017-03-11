namespace Composable.Messaging.Events
{
    interface IMutableEventDispatcher<in TEvent> : IEventDispatcher<TEvent>
        where TEvent : class
    {
        IEventHandlerRegistrar<TEvent> Register();
    }
}
