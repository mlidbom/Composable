using System;
using System.Collections.Generic;

using System.Linq;
using Composable.Contracts;
using Composable.System.Linq;

namespace Composable.System.Reflection
{
    /// <summary>A collection of extensions to work with <see cref="Type"/></summary>
    static class TypeExtensions
    {
        /// ///<returns>true if <paramref name="me"/> implements the interface: <typeparamref name="TImplemented"/>. By definition true if <paramref name="me"/> == <typeparamref name="TImplemented"/>.</returns>
        public static bool Implements<TImplemented>(this Type me)
        {
            ContractOptimized.Argument(me, nameof(me))
                             .NotNull();

            if (!typeof(TImplemented).IsInterface)
            {
                throw new ArgumentException(nameof(TImplemented));
            }

            return me.Implements(typeof(TImplemented));
        }

        ///<returns>true if <paramref name="me"/> implements the interface: <paramref name="implemented"/>. By definition true if <paramref name="me"/> == <paramref name="implemented"/>.</returns>
        public static bool Implements(this Type me, Type implemented)
        {
            ContractOptimized.Argument(me, nameof(me), implemented, nameof(implemented))
                             .NotNull();

            if(!implemented.IsInterface)
            {
                throw new ArgumentException(nameof(implemented));
            }

            if(me == implemented) { return true;}

            if(implemented.IsGenericTypeDefinition)
            {
                return
                    me.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == implemented);
            }

            return me.GetInterfaces().Contains(implemented);
        }

        ///<summary>Returns a sequence containing all the classes and interfaces that this type inherits/implements</summary>
        public static IEnumerable<Type> GetAllTypesInheritedOrImplemented(this Type me)
        {
            ContractOptimized.Argument(me, nameof(me))
                             .NotNull();
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

        static readonly Dictionary<string, Type> TypeMap = new Dictionary<string, Type>();

        ///<summary>Finds the class that the string represents within any loaded assembly. Calling with "MyNameSpace.MyObject" would return the same type as typeof(MyNameSpace.MyObject) etc.</summary>
        public static Type AsType(this string valueType)
        {
            Type type;
            if(valueType.TryGetType(out type))
            {
                return type;
            }
            throw new FailedToFindTypeException(valueType);
        }

        ///<summary>Finds the class that the string represents within any loaded assembly. Calling with "MyNameSpace.MyObject" would return the same type as typeof(MyNameSpace.MyObject) etc.</summary>
        public static bool TryGetType(this string valueType, out Type type)
        {
            lock (TypeMap)
            {
                if (TypeMap.TryGetValue(valueType, out type))
                {
                    return true;
                }

                var types = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(assembly => assembly.GetType(valueType))
                    .Where(t => t != null)
                    .ToArray();
                if (types.None())
                {
                    return false;
                }

                if (types.Count() > 1)
                {
                    throw new MultipleMatchingTypesException(valueType);
                }

                type = types.Single();
                TypeMap.Add(valueType, types.Single());
                return true;
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