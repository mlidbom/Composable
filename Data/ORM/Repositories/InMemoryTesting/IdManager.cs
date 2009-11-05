using System;
using System.Collections;

namespace Void.Data.ORM.InMemoryTesting
{
    public abstract class IdManager<TInstance, TId> : IIdManager
    {
        //Todo: exchange Setter and Getter for Member and make it work for private properties/fields
        public Func<TInstance, TId> Getter { get; set; }
        public Action<TInstance, TId> Setter { get; set; }


        public object Unsaved { get; set; }

        public object Get(object instance)
        {
            return Getter((TInstance) instance);
        }

        public void Set(object instance, object id)
        {
            Setter((TInstance) instance, (TId) id);
        }

        public abstract object NextId(IEnumerable allInstancesOfType);
    }
}