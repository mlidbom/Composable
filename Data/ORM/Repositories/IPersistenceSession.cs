using System;
using System.Linq;

namespace Void.Data.ORM
{
    public interface IPersistenceSession : IDisposable
    {
        IQueryable<T> Query<T>();
        T Get<T>(object id);
        T TryGet<T>(object id);
        void SaveOrUpdate(object instance);
        void Delete(object instance);
    }
}