namespace Composable.CQRS.EventSourcing.EventRefactoring
{
    public interface IRenameEvents
    {
        void Rename(EventNameMapping mapping);
    }
}