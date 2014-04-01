using System.Collections;
using System.Linq;

namespace Composable.Contracts
{
    public static class EnumerableInspector
    {
        ///<summary>
        /// <para>Throws <see cref="ObjectIsNullContractViolationException"/> if any inspected value is null.</para>
        /// <para>Throws a <see cref="EnumerableIsEmptyContractViolationException"/> if any inspected value is an empty sequence.</para>
        /// </summary>
        public static Inspected<TValue> NotNullOrEmptyEnumerable<TValue>(this Inspected<TValue> me)
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

    public class EnumerableIsEmptyContractViolationException : ContractViolationException
    {
        public EnumerableIsEmptyContractViolationException(InspectedValue badValue) : base(badValue) {}
    }
}
