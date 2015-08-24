namespace Composable.CQRS.EventHandling
{
    public interface IEventDispatcher<in TEvent>
    {
        void Dispatch(TEvent evt);
    }
}