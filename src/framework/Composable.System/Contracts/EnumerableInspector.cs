using System.Collections;
using System.Collections.Generic;
using System.Linq;
// ReSharper disable UnusedMethodReturnValue.Global

namespace Composable.Contracts
{
    ///<summary>Performs inspections on <see cref="IEnumerable{T}"/> instances</summary>
    static class EnumerableInspector
    {
        ///<summary>
        /// <para>Throws <see cref="ObjectIsNullContractViolationException"/> if any inspected value is null.</para>
        /// <para>Throws a <see cref="EnumerableIsEmptyContractViolationException"/> if any inspected value is an empty sequence.</para>
        /// </summary>
        public static IInspected<TValue> NotNullOrEmptyEnumerable<TValue>(this IInspected<TValue> me)
            where TValue : IEnumerable
        {
            me.Inspect(
                inspected => !ReferenceEquals(inspected, null),
                badValue => new ObjectIsNullContractViolationException(badValue));

            return me.Inspect(
                inspected => inspected.Cast<object>().Any(),
                badValue => new EnumerableIsEmptyContractViolationException(badValue));
        }
    }

    ///<summary>Thrown if an enumerable is empty but is not allowed to be.</summary>
    class EnumerableIsEmptyContractViolationException : ContractViolationException
    {
        ///<summary>Standard constructor.</summary>
        public EnumerableIsEmptyContractViolationException(IInspectedValue badValue) : base(badValue) {}
    }
}
