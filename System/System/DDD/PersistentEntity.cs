#region usings

using System;
using System.Diagnostics;

#endregion

namespace Composable.DDD
{
    public class IdEqualityObject<TEntity> : IEquatable<TEntity> where TEntity : IdEqualityObject<TEntity>
    {
        protected IdEqualityObject(Guid id)
        {
            Id = id;
        }

        /// <summary>Implements: <see cref="IPersistentEntity{TKeyType}.Id"/></summary>
        public virtual Guid Id { get; private set; }

        protected void SetIdBeVerySureYouKnowWhatYouAreDoing(Guid id)
        {
            Id = id;
        }

        /// <summary>
        /// Implements equals using persistent reference semantics.
        /// If two instances have the same Id, Equals will return true.
        /// </summary>
        public virtual bool Equals(TEntity other)
        {
            return !ReferenceEquals(null, other) && other.Id.Equals(Id);
        }

        /// <summary>
        /// Implements equals using persistent reference semantics.
        /// If two instances have the same Id, Equals will return true.
        /// </summary>
        public override bool Equals(object other)
        {
            return (other is TEntity) && Equals((TEntity)((object)((TEntity)other)));
        }

        /// <summary>Implements: <see cref="object.GetHashCode"/></summary>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        ///<summary>True if both instances have the same ID</summary>
        public static bool operator ==(IdEqualityObject<TEntity> lhs, IdEqualityObject<TEntity> rhs)
        {
            if (ReferenceEquals(lhs, rhs))
            {
                return true;
            }

            return !ReferenceEquals(null, lhs) && lhs.Equals(rhs);
        }

        ///<summary>True if both instances do not have the same ID</summary>
        public static bool operator !=(IdEqualityObject<TEntity> lhs, IdEqualityObject<TEntity> rhs)
        {
            return !(lhs == rhs);
        }
    }

    /// <summary>
    /// Simple base class for Entities that ensures a correct identity based <see cref="object.Equals(object)"/>, <see cref="object.GetHashCode"/>, and <see cref="IEquatable{T}"/>.
    /// 
    /// This class uses <see cref="Guid"/>s as Ids because it is the only built in .Net type the developers are
    /// avare of which can, in practice, guarantee for a system that an PersistentEntity will have a globally unique immutable identity 
    /// from the moment of instantiation and through any number of persisting-loading cycles. That in turn is an 
    /// absolute requirement for a correct implementation of <see cref="object.Equals(object)"/>, 
    /// <see cref="object.GetHashCode"/>, and <see cref="IEquatable{TEntity}"/>.
    /// </summary>
    [DebuggerDisplay("{GetType().Name} Id={Id}")]
    [Serializable]
    public class PersistentEntity<TEntity> : IdEqualityObject<TEntity>, IPersistentEntity<Guid> where TEntity : PersistentEntity<TEntity>
    {
        /// <summary>
        /// Creates an instance using the supplied <paramref name="id"/> as the Id.
        /// </summary>
        protected PersistentEntity(Guid id):base(id)
        {
            SetIdBeVerySureYouKnowWhatYouAreDoing(id);
        }

        /// <summary>
        /// Creates a new instance with an automatically generated Id
        /// </summary>
        public PersistentEntity():base(Guid.NewGuid())
        {
        }

        ///<summary>True if both instances have the same ID</summary>
        public static bool operator ==(PersistentEntity<TEntity> lhs, PersistentEntity<TEntity> rhs)
        {
            if (ReferenceEquals(lhs, rhs))
            {
                return true;
            }

            return !ReferenceEquals(null, lhs) && lhs.Equals(rhs);
        }

        ///<summary>True if both instances do not have the same ID</summary>
        public static bool operator !=(PersistentEntity<TEntity> lhs, PersistentEntity<TEntity> rhs)
        {
            return !(lhs == rhs);
        }
    }
}