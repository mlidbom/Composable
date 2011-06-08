#region usings

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.System.Linq;

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

        public static IEnumerable<Type> GetAllTypesInheritedOrImplemented(this Type me)
        {
            return me.GetClassInheritanceChain().Concat(me.GetInterfaces());
        }

        public static IEnumerable<Type> GetClassInheritanceChain(this Type me)
        {
            yield return me;
            while (me.BaseType != null)
            {
                me = me.BaseType;
                yield return me;
            }
        }


        private static readonly Dictionary<string, Type> _typeMap = new Dictionary<string, Type>();
        public static Type AsType(this string valueType)
        {
            Type type;
            if (_typeMap.TryGetValue(valueType, out type))
            {
                return type;
            }

            var types = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(valueType))
                .Where(t => t != null)
                .ToArray();
            if (types.None())
            {
                throw new FailedToFindTypeException(valueType);
            }

            if (types.Count() > 1)
            {
                throw new MultipleMatchingTypesException(valueType);
            }

            type = types.Single();
            _typeMap.Add(valueType, types.Single());
            return type;
        }

        public class MultipleMatchingTypesException : Exception
        {
            public MultipleMatchingTypesException(string typeName)
                : base(typeName)
            {
            }
        }

        public class FailedToFindTypeException : Exception
        {
            public FailedToFindTypeException(string typeName)
                : base(typeName)
            {
            }
        }
    }
}