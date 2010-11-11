#region usings

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

#endregion

namespace Void.Reflection
{
    public static class TypeExtensions
    {
        public static bool Implements(this Type me, Type implemented)
        {
            Contract.Requires(me != null && implemented != null && implemented.IsInterface);

            if (implemented.IsGenericTypeDefinition)
            {
                return
                    me.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == implemented);
            }

            return implemented.IsAssignableFrom(me);
        }
    }
}