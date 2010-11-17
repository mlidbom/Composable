using System;

namespace Composable.DDD
{
    ///<summary>Base class for persistent entities with versioning information</summary>
    [Serializable]
    public class VersionedPersistentEntity<T> : PersistentEntity<T> where T : VersionedPersistentEntity<T>
    {
        protected VersionedPersistentEntity(Guid id):base(id){}

        protected VersionedPersistentEntity(){}

        ///<summary>Contains the current version of the entity</summary>
        public virtual int Version { get; private set; }
    }
}