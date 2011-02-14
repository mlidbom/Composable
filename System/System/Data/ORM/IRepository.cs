using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using Composable.CQRS;
using Composable.System.Linq;

namespace Composable.Data.ORM
{
    [ContractClass(typeof(RepositoryContract<,>))]
    public interface IRepository<TInstance, in TKey> : IQueryable<TInstance>
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

    [ContractClassFor(typeof(IRepository<,>))]
    internal abstract class RepositoryContract<TInstance, TKey> : IRepository<TInstance, TKey>
    {
        public IEnumerator<TInstance> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public Expression Expression { get { throw new NotImplementedException(); } }

        public Type ElementType { get { throw new NotImplementedException(); } }

        public IQueryProvider Provider { get { throw new NotImplementedException(); } }

        public TInstance Get(TKey id)
        {
            Contract.Requires(id!=null);
            throw new NotImplementedException();
        }

        public TInstance TryGet(TKey id)
        {
            Contract.Requires(id != null);
            throw new NotImplementedException();
        }

        public bool TryGet(TKey id, out TInstance result)
        {
            Contract.Requires(id != null);
            throw new NotImplementedException();
        }

        public IList<TInstance> GetAll(IEnumerable<TKey> ids)
        {
            Contract.Requires(ids != null);
            throw new NotImplementedException();
        }

        public IList<TInstance> TryGetAll(IEnumerable<TKey> ids)
        {
            Contract.Requires(ids != null);
            throw new NotImplementedException();
        }

        public void SaveOrUpdate(TInstance instance)
        {
            Contract.Requires(instance != null);
            throw new NotImplementedException();
        }

        public void SaveOrUpdate(IEnumerable<TInstance> instances)
        {
            Contract.Requires(instances != null);
            throw new NotImplementedException();
        }

        public void Delete(TInstance instance)
        {
            Contract.Requires(instance != null);
            throw new NotImplementedException();
        }

        public IQueryable<TInstance> Find(IFilter<TInstance> criteria)
        {
            Contract.Requires(criteria != null);
            throw new NotImplementedException();
        }
    }
}