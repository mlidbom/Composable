using System;

namespace Void.Data.ORM.Domain
{
    /// <summary>
    /// Simple base class for Entities that ensures a correct identity based <see cref="object.Equals(object)"/>, <see cref="object.GetHashCode"/>, and <see cref="IEquatable{TEntity}"/>.
    /// 
    /// This class uses <see cref="Guid"/>s as Ids because it is the only built in .Net type the developers are
    /// avare of that can guarantee for a system that an Entity will have a globally unique immutable identity 
    /// from the moment of instantiation and through any number of persisting-loading cycles. That in turn is an 
    /// absolute requirement for a correct implementation of <see cref="object.Equals(object)"/>, 
    /// <see cref="object.GetHashCode"/>, and <see cref="IEquatable{TEntity}"/>.
    /// 
    /// However, this does not mean that you must use the Guid as the database primary key if you
    /// persist instances of your inheriting type. See <see cref="EntityWithSurrogateKey{TEntity,TKey}"/> for an alternative.
    /// </summary>
    public class Entity<TEntity> : IEntity, IEquatable<TEntity> where TEntity : Entity<TEntity>
    {
        public Entity()
        {
            Id = Guid.NewGuid();
        }

        public virtual Guid Id { get; private set; }

        object IEntity.Id { get { return Id; } }

        public virtual bool Equals(TEntity other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            return other.Id.Equals(Id);
        }

        public override bool Equals(object other)
        {
            return (other is TEntity) && Equals((TEntity) other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}