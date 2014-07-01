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

        ///<summary>Returns a sequence containing all the classes and interfaces that this type inherits/implements</summary>
        public static IEnumerable<Type> GetAllTypesInheritedOrImplemented(this Type me)
        {
            Contract.Requires(me != null);
            return me.GetClassInheritanceChain().Concat(me.GetInterfaces());
        }

        ///<summary>Lists all classes that this class inherits prepended by the class of the instance itself.</summary>
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

        ///<summary>Finds the class that the string represents within any loaded assembly. Calling with "MyNameSpace.MyObject" would return the same type as typeof(MyNameSpace.MyObject) etc.</summary>
        public static Type AsType(this string valueType)
        {
            lock (_typeMap)
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
        }

        ///<summary>Thrown if there is more than one type that matches the string passed to <see cref="TypeExtensions.AsType"/></summary>
        public class MultipleMatchingTypesException : Exception
        {
            internal MultipleMatchingTypesException(string typeName): base(typeName)
            {
            }
        }

        ///<summary>Thrown if there is no type that matches the string passed to <see cref="TypeExtensions.AsType"/> is found</summary>
        public class FailedToFindTypeException : Exception
        {
            internal FailedToFindTypeException(string typeName): base(typeName)
            {
            }
        }
    }
}