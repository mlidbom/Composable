using System.Diagnostics.Contracts;

namespace Composable.DDD
{
    /// <summary>
    /// Anything that can be uniquely identified using it's id over any number of persist/load cycles.
    /// </summary>
    [ContractClass(typeof(HasPersistentIdentityContract<>))]
    public interface IHasPersistentIdentity<out TKeyType>
    {
        /// <summary>The unique identifier for this instance.</summary>
        TKeyType Id { get; }
    }


    [ContractClassFor(typeof (IHasPersistentIdentity<>))]
    internal abstract class HasPersistentIdentityContract<T> : IHasPersistentIdentity<T>
    {
        [ContractInvariantMethod]
        private void Invariants()
        {
            Contract.Invariant(!Equals(((IHasPersistentIdentity<T>)this).Id, default(T)));
        }

        T IHasPersistentIdentity<T>.Id
        {
            get
            {
                Contract.Ensures(!Equals(Contract.Result<T>(), default(T)), "IHasPersistentIdentity<T>.Id may must never be default(T)");
                return default(T);
            }
        }
    }
}