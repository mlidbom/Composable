using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

//Fixme: does not belong here. Move entire namespace and rename
namespace Composable.System.Wrappers
{
    ///<summary/>
    [Pure]
    public static class WrapperExtensions
    {
        /// <summary>
        /// Given a sequence of <see cref="IWrapper{T}"/> returns a sequence containing the wrapped T values.
        /// </summary>
        public static IEnumerable<T> Unwrap<T>(this IEnumerable<IWrapper<T>> wrapper)
        {
            Contract.Requires(wrapper != null);
            return wrapper.Select(wrapping => wrapping.Wrapped);
        }
    }
}