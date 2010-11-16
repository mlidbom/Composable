using System.Collections.Generic;
using System.Linq;
using Composable.System.Linq;

namespace Composable.Data.ORM
{
    public interface IRepository<TInstance, TKey> : IQueryable<TInstance>
    {
        TInstance Get(TKey id);
        TInstance TryGet(TKey id);
        bool TryGet(TKey id, out TInstance result);

        IList<TInstance> GetAll(IEnumerable<TKey> ids);                
        IList<TInstance> TryGetAll(IEnumerable<TKey> ids);

        void SaveOrUpdate(TInstance instance);
        void SaveOrUpdate(IEnumerable<TInstance> instances);       

        void Delete(TInstance instance);
        IQueryable<TInstance> Find(IFilter<TInstance> criteria);
    }
}