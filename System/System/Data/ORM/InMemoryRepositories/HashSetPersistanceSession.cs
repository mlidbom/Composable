using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Composable.Data.ORM.InMemoryRepositories
{
    public class HashSetPersistanceSession : IPersistenceSession
    {
        private readonly HashSet<object> _data = new HashSet<object>();
        private IDictionary<Type, IIdManager> IdManagers { get; set; }

        public HashSetPersistanceSession(IDictionary<Type, IIdManager> idManagers)
        {
            IdManagers = idManagers;
        }

        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_data!=null);
        }

        public IQueryable<T> Query<T>()
        {
            return _data.OfType<T>().AsQueryable();
        }

        public T Get<T>(object id)
        {
            var idManager = IdManagers[typeof(T)];
            return DoQuery<T>(me => Equals(idManager.Get(me), id)).Single();
        }

        public T TryGet<T>(object id)
        {
            var idManager = IdManagers[typeof(T)];
            return DoQuery<T>(me => Equals(idManager.Get(me), id)).SingleOrDefault();
        }

        public void Evict(object instance)
        {
            //null op.
        }

        public void SaveOrUpdate(object instance)
        {
            var idManager = IdManagers[instance.GetType()];
            if(Equals(idManager.Get(instance), idManager.Unsaved))
            {
                idManager.Set(instance, idManager.NextId(AllInstancesOfType(instance.GetType())));
            }
            _data.Add(instance);
        }

        private IEnumerable AllInstancesOfType(Type type)
        {
            return _data.Where(me => Equals(me.GetType(), type));
        }

        public void Delete(object instance)
        {
            _data.Remove(instance);
        }

        private IEnumerable<T> DoQuery<T>(Predicate<object> predicate)
        {
            return _data.Where(me => predicate(me)).OfType<T>();
        }

        #region Implementation of IDisposable

        public void Dispose()
        {
            _data.Clear();
        }

        #endregion
    }
}