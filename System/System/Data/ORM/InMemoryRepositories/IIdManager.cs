using System.Collections;

namespace Composable.Data.ORM.InMemoryRepositories
{
    public interface IIdManager
    {
        object Unsaved { get; }
        object Get(object instance);
        object NextId(IEnumerable allInstancesOfType);
        void Set(object instance, object id);
    }
}