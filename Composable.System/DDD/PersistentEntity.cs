#region usings

using System;
using System.Diagnostics;
using Composable.System;

#endregion

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
    [DebuggerDisplay("{ToString()}")]
    public class IdEqualityObject<TEntity, TKEy> : IEquatable<TEntity>, IHasPersistentIdentity<TKEy> where TEntity : IdEqualityObject<TEntity, TKEy>
    {
        ///<summary>Construct an instance with <param name="id"> as the <see cref="Id"/></param>.</summary>
        protected IdEqualityObject(TKEy id)
        {
            Id = id;
        }

        TKEy _id;

        /// <inheritdoc />
        public virtual TKEy Id { get { return _id; } private set { _id = value; } }

        ///<summary>Sets the id of the instance. Should probably never be used except by infrastructure code.</summary>
        protected void SetIdBeVerySureYouKnowWhatYouAreDoing(TKEy id)
        {
            Id = id;
        }

        ///<summary>Gets the id of the instance bypassing contract validation. Should probably never be used except by infrastructure code.</summary>
        protected TKEy GetIdBypassContractValidation()
        {
            return _id;
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
            return Equals(other as TEntity);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        ///<summary>True if both instances have the same ID</summary>
        public static bool operator ==(IdEqualityObject<TEntity, TKEy> lhs, IdEqualityObject<TEntity, TKEy> rhs)
        {
            if (ReferenceEquals(lhs, rhs))
            {
                return true;
            }

            return !ReferenceEquals(null, lhs) && lhs.Equals(rhs);
        }

        ///<summary>True if both instances do not have the same ID</summary>
        public static bool operator !=(IdEqualityObject<TEntity, TKEy> lhs, IdEqualityObject<TEntity, TKEy> rhs)
        {
            return !(lhs == rhs);
        }

        ///<summary>Returns a string similar to: MyType:MyId</summary>
        public override string ToString()
        {
            return "{0}:{1}".FormatWith(GetType().Name, Id);
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
#pragma warning disable 660,661
    public class PersistentEntity<TEntity> : IdEqualityObject<TEntity, Guid>, IPersistentEntity<Guid> where TEntity : PersistentEntity<TEntity>
#pragma warning restore 660,661
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