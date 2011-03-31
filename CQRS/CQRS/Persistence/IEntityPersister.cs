namespace Composable.Persistence
{
    public interface IEntityPersister
    {
        void Save(object instance);
        void Delete(object instance);
    }
}