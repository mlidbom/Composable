namespace Composable.Messaging.Events
{
    interface IEventDispatcher<in TEvent>
    {
        void Dispatch(TEvent evt);
    }
}
