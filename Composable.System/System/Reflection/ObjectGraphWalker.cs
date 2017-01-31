using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Composable.System.Reflection
{
    //todo:tests and make public and remove ignoring of unused members
    ///<summary>Flattens an object graph into a sequence by walking instance members using reflection.</summary>
    // ReSharper disable once UnusedMember.Global
    static class ObjectGraphWalker
    {
        ///<summary>Flattens the supplied object graph into a sequence</summary>
        public static IEnumerable<object> GetGraph(object o)
        {
            var collected = new List<object>();
            InternalGetGraph(o, collected);
            return collected;
        }

        private static void InternalGetGraph(object o, List<Object> collected)
        {
            Contract.Requires(collected != null);

            if (o == null)
            {
                return;
            }
            
            collected.Add(o);

            var objectType = o.GetType();
            if (objectType.IsPrimitive || o is string || o is DateTime || o is Guid)
            {
                return;
            }

            if (o is IEnumerable)
            {
                foreach (var value in (o as IEnumerable))
                {
                    InternalGetGraph(value, collected);
                }
            }
            else
            {
                foreach (var value in MemberAccessorHelper.GetFieldAndPropertyValues(o))
                {
                    InternalGetGraph(value, collected);
                }
            }
        }
    }
}