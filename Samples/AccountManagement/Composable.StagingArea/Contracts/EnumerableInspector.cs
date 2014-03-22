using System.Collections;
using System.Linq;

namespace Composable.Contracts
{
    public static class EnumerableInspector
    {
        public static Inspected<TArgument> NotNullOrEmpty<TArgument>(this Inspected<TArgument> me)
            where TArgument : IEnumerable
        {
            me.Inspect(
                inspected => !ReferenceEquals(inspected, null),
                badValue => new ObjectIsNullException(badValue.Name));

            return me.Inspect(
                inspected => inspected.Cast<object>().Any(),
                badValue => new EnumerableIsEmptyException(badValue.Name));
        }
    }

    public class EnumerableIsEmptyException : ContractException
    {
        public EnumerableIsEmptyException(string name) : base(name) {}
    }
}
