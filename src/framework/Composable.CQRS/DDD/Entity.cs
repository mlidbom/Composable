using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Composable.Contracts;

namespace Composable.DDD
{
    /// <summary>
    /// Base class for any class that considers equality to be that the Ids for two instances are the same.
    /// 
    /// It provides implementations of  <see cref="object.Equals(object)"/>, <see cref="object.GetHashCode"/>, and <see cref="IEquatable{T}"/>.
    /// 
    /// Equals is implemented as: return <code>!ReferenceEquals(null, other) &amp;&amp; other.Id.Equals(Id)</code>
    /// the operators simply uses Equals.
    /// 
    /// </summary>
    [DebuggerDisplay("{" + nameof(ToString) + "()}")]
    public class Entity<TEntity, TKEy> : IEquatable<TEntity>, IHasPersistentIdentity<TKEy> where TEntity : Entity<TEntity, TKEy>
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        ///<summary>Construct an instance with <param name="id"> as the <see cref="Id"/></param>.</summary>
        protected Entity(TKEy id) => Id = id;
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

        TKEy _id;

        /// <inheritdoc />
        [NotNull]public virtual TKEy Id
        {
            get => _id;
            private set => _id = Assert.Argument.NotNull(value);
        }

        ///<summary>Sets the id of the instance. Should probably never be used except by infrastructure code.</summary>
        protected void SetIdBeVerySureYouKnowWhatYouAreDoing(TKEy id)
        {
            Id = id;
        }

        ///<summary>Gets the id of the instance bypassing contract validation. Should probably never be used except by infrastructure code.</summary>
        protected TKEy GetIdBypassContractValidation() => _id;

        /// <summary>
        /// Implements equals using persistent reference semantics.
        /// If two instances have the same Id, Equals will return true.
        /// </summary>
        public virtual bool Equals(TEntity other) => !(other is null) && other.Id.Equals(Id);

        /// <summary>
        /// Implements equals using persistent reference semantics.
        /// If two instances have the same Id, Equals will return true.
        /// </summary>
        public override bool Equals(object other) => (other is TEntity actualOther) && Equals(actualOther);

        /// <inheritdoc />
        public override int GetHashCode() => Id.GetHashCode();

        ///<summary>True if both instances have the same ID</summary>
        public static bool operator ==(Entity<TEntity, TKEy> lhs, Entity<TEntity, TKEy> rhs)
        {
            if (ReferenceEquals(lhs, rhs))
            {
                return true;
            }

            return !(lhs is null) && lhs.Equals(rhs);
        }

        ///<summary>True if both instances do not have the same ID</summary>
        public static bool operator !=(Entity<TEntity, TKEy> lhs, Entity<TEntity, TKEy> rhs) => !(lhs == rhs);

        ///<summary>Returns a string similar to: MyType:MyId</summary>
        public override string ToString() => $"{GetType().Name}:{Id}";
    }

    /// <summary>
    /// Simple base class for Entities that ensures a correct identity based <see cref="object.Equals(object)"/>, <see cref="object.GetHashCode"/>, and <see cref="IEquatable{T}"/>.
    /// 
    /// This class uses <see cref="Guid"/>s as Ids because it is the only built in .Net type the developers are
    /// aware of which can, in practice, guarantee for a system that an PersistentEntity will have a globally unique immutable identity
    /// from the moment of instantiation and through any number of persisting-loading cycles. That in turn is an
    /// absolute requirement for a correct implementation of <see cref="object.Equals(object)"/>,
    /// <see cref="object.GetHashCode"/>, and <see cref="IEquatable{TEntity}"/>.
    /// </summary>
    [DebuggerDisplay("{GetType().Name} Id={Id}")]
#pragma warning disable 660,661
    public class Entity<TEntity> : Entity<TEntity, Guid>, IPersistentEntity<Guid> where TEntity : Entity<TEntity>
#pragma warning restore 660,661
    {
        /// <summary>
        /// Creates an instance using the supplied <paramref name="id"/> as the Id.
        /// </summary>
        protected Entity(Guid id):base(id)
        {
            SetIdBeVerySureYouKnowWhatYouAreDoing(id);
        }

        /// <summary>
        /// Creates a new instance with an automatically generated Id
        /// </summary>
        protected Entity():base(Guid.NewGuid())
        {
        }

        ///<summary>True if both instances have the same ID</summary>
        public static bool operator ==(Entity<TEntity> lhs, Entity<TEntity> rhs)
        {
            if (ReferenceEquals(lhs, rhs))
            {
                return true;
            }

            return !(lhs is null) && lhs.Equals(rhs);
        }

        ///<summary>True if both instances do not have the same ID</summary>
        public static bool operator !=(Entity<TEntity> lhs, Entity<TEntity> rhs) => !(lhs == rhs);
    }
}