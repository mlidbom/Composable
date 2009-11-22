using System.Collections.Generic;
using System.Linq;

namespace Void.Wrappers
{
    ///<summary/>
    public static class WrapperExtensions
    {
        public static IEnumerable<T> Unwrap<T>(this IEnumerable<IWrapper<T>> wrapper)
        {
            return wrapper.Select(wrapping => wrapping.Wrapped);
        }
    }
}