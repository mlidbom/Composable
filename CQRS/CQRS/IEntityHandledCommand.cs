namespace Composable.CQRS
{
    public interface IEntityHandledCommand
    {
        object EntityId { get; }
    }
}