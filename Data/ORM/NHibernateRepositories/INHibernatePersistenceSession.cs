using NHibernate;

namespace Void.Data.ORM.NHibernate
{
    public interface INHibernatePersistenceSession : IPersistenceSession
    {
        IQuery CreateQuery(string query);
        void Clear();
        void Evict(object instance);
        void CreateDataBase();
    }
}