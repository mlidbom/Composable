using System;

namespace Void
{
    /// <summary>
    /// Simple base class for Entities that ensures a correct identity based <see cref="object.Equals(object)"/>, <see cref="object.GetHashCode"/>, and <see cref="IEquatable{TEntity}"/>.
    /// 
    /// This class uses <see cref="Guid"/>s as Ids because it is the only built in .Net type the developers are
    /// avare of which can, in practice, guarantee for a system that an PersistentEntity will have a globally unique immutable identity 
    /// from the moment of instantiation and through any number of persisting-loading cycles. That in turn is an 
    /// absolute requirement for a correct implementation of <see cref="object.Equals(object)"/>, 
    /// <see cref="object.GetHashCode"/>, and <see cref="IEquatable{TEntity}"/>.
    /// </summary>
    public class PersistentEntity<TEntity> : IPersistentEntity<Guid>, IEquatable<TEntity> where TEntity : PersistentEntity<TEntity>
    {
        /// <summary>
        /// Creates a new instance with an automatically generated Id
        /// </summary>
        public PersistentEntity()
        {
            Id = Guid.NewGuid();
        }

        /// <summary>Implements: <see cref="IPersistentEntity{TKeyType}.Id"/></summary>
        public virtual Guid Id { get; private set; }

        /// <summary>
        /// Implements equals using persistent reference semantics.
        /// If two instances have the same Id, Equals will return true.
        /// </summary>
        public virtual bool Equals(TEntity other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            return other.Id.Equals(Id);
        }

        /// <summary>
        /// Implements equals using persistent reference semantics.
        /// If two instances have the same Id, Equals will return true.
        /// </summary>
        public override bool Equals(object other)
        {
            return (other is TEntity) && Equals((TEntity) other);
        }

        /// <summary>Implements: <see cref="object.GetHashCode"/></summary>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}