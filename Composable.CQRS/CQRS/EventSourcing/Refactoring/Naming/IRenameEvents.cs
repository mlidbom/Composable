namespace Composable.CQRS.EventSourcing.Refactoring.Naming
{
    public interface IRenameEvents
    {
        void Rename(EventNameMapping mapping);
    }
}