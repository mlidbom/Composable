#region usings

using System;

#endregion

namespace Composable.DDD
{
    ///<summary>Base class for persistent entities with versioning information</summary>
    [Serializable]
    public class VersionedPersistentEntity<T> : PersistentEntity<T> where T : VersionedPersistentEntity<T>
    {
        /// <summary>Creates an instance using the supplied <paramref name="id"/> as the Id.</summary>
        protected VersionedPersistentEntity(Guid id) : base(id)
        {
        }

        /// <summary> Creates an instance using a newly generated Id</summary>
        VersionedPersistentEntity()
        {
        }

        ///<summary>Contains the current version of the entity</summary>
        public virtual int Version { get; protected set; }
    }
}