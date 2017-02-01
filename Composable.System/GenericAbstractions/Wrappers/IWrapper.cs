#region usings

using System;
using System.Diagnostics.Contracts;

#endregion

namespace Composable.GenericAbstractions.Wrappers
{
    /// <summary>
    /// Represents the generic concept of a type that extends another type by containing a value of the other type.
    /// </summary>
    [ContractClass(typeof(WrapperContract<>))]
    public interface IWrapper<T>
    {
        ///<summary>The wrapped value.</summary>
        T Wrapped { get; }
    }

    [ContractClassFor(typeof(IWrapper<>))] abstract class WrapperContract<T> : IWrapper<T>
    {
        public T Wrapped
        {
            get
            {
                Contract.Ensures(Contract.Result<T>() != null);
                return default(T);
            }
        }
    }
}