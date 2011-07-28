using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Composable.System.Reflection
{
    public static class ObjectGraphWalker
    {
        public static IEnumerable<object> GetGraph(object o)
        {
            return InternalGetGraph(o);
        }

        private static IEnumerable<object> InternalGetGraph(object o, Func<object, object>[] potentialGetters = null, Type getterType = null)
        {
            if (o == null)
            {
                yield break;
            }
            
            yield return o;

            var objectType = o.GetType();
            if (objectType.IsPrimitive || o is string || o is DateTime || o is Guid)
            {
                yield break;
            }

            if (o is IEnumerable)
            {
                var enumerable = (o as IEnumerable).Cast<object>().ToList();
                if (enumerable.Any())
                {
                    var firstType = enumerable.First().GetType();
                    var firstGetters = MemberAccessorHelper.GetFieldsAndPropertyGetters(firstType);

                    foreach (var value in enumerable.SelectMany(k => InternalGetGraph(k, firstGetters, firstType)))
                    {
                        yield return value;
                    }
                }
            }
            else
            {
                if(potentialGetters == null || objectType != getterType)
                {
                    potentialGetters = MemberAccessorHelper.GetFieldsAndPropertyGetters(objectType);
                }

                foreach (var value in potentialGetters.Select(getter => getter(o)).SelectMany(GetGraph))
                {
                    yield return value;
                }
            }
        }
    }
}