#region usings

using System;
using System.Diagnostics.Contracts;
using System.Linq;

#endregion

namespace Composable.System.Reflection
{
    /// <summary>A collection of extensions to work with <see cref="Type"/></summary>
    public static class TypeExtensions
    {
        ///<returns>true if <paramref name="me"/> implements the interface <typeparamref name="TImplemented"/></returns>
        public static bool Implements<TImplemented>(this Type me)
        {
            Contract.Requires(me != null);
            Contract.Requires(typeof(TImplemented).IsInterface);
            return me.Implements(typeof(TImplemented));
        }

        ///<returns>true if <paramref name="me"/> implements the interface: <paramref name="implemented"/></returns>
        public static bool Implements(this Type me, Type implemented)
        {
            Contract.Requires(me != null);
            Contract.Requires(implemented != null);
            Contract.Requires(implemented.IsInterface);

            if(implemented.IsGenericTypeDefinition)
            {
                return
                    me.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == implemented);
            }

            return implemented.IsAssignableFrom(me);
        }
    }
}