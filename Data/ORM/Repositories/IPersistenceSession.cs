using System;
using System.Linq;

namespace Void.Data.ORM
{
    public interface IPersistenceSession : IDisposable
    {
        IQueryable<T> Linq<T>();
        T Get<T>(object id);
        T TryGet<T>(object id);
        void SaveOrUpdate(object instance);
        void Delete(object instance);
    }
}