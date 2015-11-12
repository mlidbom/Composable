namespace Composable.CQRS.EventSourcing.EventRefactoring.Naming
{
    public interface IRenameEvents
    {
        void Rename(EventNameMapping mapping);
    }
}