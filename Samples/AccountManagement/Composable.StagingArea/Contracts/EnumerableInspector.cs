using System.Collections;
using System.Linq;

namespace Composable.Contracts
{
    public static class EnumerableInspector
    {
        public static Inspected<TValue> NotNullOrEmptyEnumerable<TValue>(this Inspected<TValue> me)
            where TValue : IEnumerable
        {
            me.Inspect(
                inspected => !ReferenceEquals(inspected, null),
                badValue => new ObjectIsNullException(badValue));

            return me.Inspect(
                inspected => inspected.Cast<object>().Any(),
                badValue => new EnumerableIsEmptyException(badValue));
        }
    }

    public class EnumerableIsEmptyException : ContractException
    {
        public EnumerableIsEmptyException(InspectedValue badValue) : base(badValue) {}
    }
}
