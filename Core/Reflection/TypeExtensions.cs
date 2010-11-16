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
        public static bool Implements<TImplemented>(this Type me)
        {
            Contract.Requires(me != null && typeof(TImplemented).IsInterface);
            return me.Implements(typeof(TImplemented));
        }

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