using System.Collections;

namespace Void.Data.ORM.InMemoryTesting
{
    public interface IIdManager
    {
        object Unsaved { get; }
        object Get(object instance);
        object NextId(IEnumerable allInstancesOfType);
        void Set(object instance, object id);
    }
}