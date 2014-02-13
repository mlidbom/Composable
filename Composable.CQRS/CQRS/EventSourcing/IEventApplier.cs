namespace Composable.CQRS.EventSourcing
{
    public interface IEventApplier<in TEvent>
    {
        void Apply(TEvent evt);
    }
}
