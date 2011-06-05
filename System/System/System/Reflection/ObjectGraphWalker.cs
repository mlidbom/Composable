using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Composable.System.Reflection
{
    public static class ObjectGraphWalker
    {
        public static IEnumerable<object> GetGraph(object o)
        {
            yield return o;
            if(o is IEnumerable)
            {
                foreach (var value in (o as IEnumerable).Cast<object>().SelectMany(GetGraph))
                {
                    yield return value;
                }
            }else
            {
                foreach (var value in MemberAccessorHelper.GetFieldAndPropertyValues(o))
                {
                    yield return value;
                }
            }
                        
        }
    }
}