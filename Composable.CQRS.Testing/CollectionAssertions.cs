using System.Collections;
using System.Linq;
using Composable.System.Linq;
using NUnit.Framework;

namespace Composable.CQRS.Testing
{
    public static class CollectionAssertions
    {
        public static void AssertContainsSingle<T>(this IEnumerable me)
        {
            var result = me.Cast<object>().OfType<T>();
            if(result.None())
            {
                Assert.Fail("Expected collection to contain Single instance of {0} but no instance was found", typeof(T));
            }if(result.Count() > 1)
            {
                Assert.Fail("Expected collection to contain Single instance of {0} but {1} instances were found", typeof(T), result.Count());
            }
        }
    }
}