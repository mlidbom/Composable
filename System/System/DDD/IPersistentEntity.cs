#region usings

using System;
using System.Diagnostics.Contracts;

#endregion

namespace Composable.DDD
{
    /// <summary>
    /// Should be implemented by persistent* classes that represents entities in the Domain Driven Design sense of the word.
    /// 
    /// The vital distinction about Persistent Entities is that equality is defined by Identity, 
    /// and as such they must guarantee that they have a non-default identity at all times.  
    /// 
    /// * Classes that have a lifecycle longer than an application run. Often persisted in databases.
    /// </summary>
    /// <typeparam name="TKeyType"></typeparam>
    [ContractClass(typeof(PersistentEntityContract<>))]
    public interface IPersistentEntity<out TKeyType> : IHasPersistentIdentity<TKeyType>
    {        
    }

    [ContractClassFor(typeof(IPersistentEntity<>))]
    internal abstract class PersistentEntityContract<T> : IPersistentEntity<T>
    {
        [ContractInvariantMethod]
        private void Invariants()
        {
            Contract.Invariant(!Equals(((IPersistentEntity<T>)this).Id, default(T)));
        }

        T IHasPersistentIdentity<T>.Id
        {
            get
            {
                Contract.Ensures(!Equals(Contract.Result<T>(), default(T)));
                return default(T);
            }
        }
    }
}