using System;

namespace Composable.DDD
{
    ///<summary>Base class for persistent entities with versioning information</summary>
    public class VersionedEntity<T> : Entity<T> where T : VersionedEntity<T>
    {
        /// <summary>Creates an instance using the supplied <paramref name="id"/> as the Id.</summary>
        protected VersionedEntity(Guid id) : base(id)
        {
        }

        ///<summary>Contains the current version of the entity</summary>
        public virtual int Version { get; protected set; }
    }
}