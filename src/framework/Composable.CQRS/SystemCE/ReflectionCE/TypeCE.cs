using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Composable.Contracts;
using Composable.SystemCE.LinqCE;

namespace Composable.SystemCE.ReflectionCE
{
    /// <summary>A collection of extensions to work with <see cref="Type"/></summary>
    static class TypeCE
    {
        public static string FullNameNotNull(this Type @this) => Contract.ReturnNotNull(@this.FullName);
        public static Type DeclaringTypeNotNull(this Type @this) => Contract.ReturnNotNull(@this.DeclaringType);

        /// ///<returns>true if <paramref name="me"/> implements the interface: <typeparamref name="TImplemented"/>. By definition true if <paramref name="me"/> == <typeparamref name="TImplemented"/>.</returns>
        public static bool Implements<TImplemented>(this Type me)
        {
            Contract.ArgumentNotNull(me, nameof(me));

            if (!typeof(TImplemented).IsInterface)
            {
                throw new ArgumentException(nameof(TImplemented));
            }

            return typeof(TImplemented).IsAssignableFrom(me);
        }

        ///<returns>true if <paramref name="me"/> implements the interface: <paramref name="implemented"/>. By definition true if <paramref name="me"/> == <paramref name="implemented"/>.</returns>
        public static bool Implements(this Type me, Type implemented)
        {
            Contract.ArgumentNotNull(me, nameof(me), implemented, nameof(implemented));

            if(!implemented.IsInterface)
            {
                throw new ArgumentException(nameof(implemented));
            }

            if(me == implemented) { return true;}

            if(me.IsInterface && me.IsGenericType && me.GetGenericTypeDefinition() == implemented)
            {
                return true;
            }

            if(implemented.IsGenericTypeDefinition)
            {
                return
                    me.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == implemented);
            }

            return me.GetInterfaces().Contains(implemented);
        }

        internal static Type GetGenericInterface(this Type me, Type implemented)
        {
            Assert.Argument.Assert(me != null, implemented != null).And(implemented.IsGenericTypeDefinition);

            return me.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == implemented);
        }

        static readonly Dictionary<string, Type> TypeMap = new Dictionary<string, Type>();

        ///<summary>Finds the class that the string represents within any loaded assembly. Calling with "MyNameSpace.MyObject" would return the same type as typeof(MyNameSpace.MyObject) etc.</summary>
        public static Type AsType(this string valueType)
        {
            if (valueType.TryGetType(out var type))
            {
                return type;
            }
            throw new FailedToFindTypeException(valueType);
        }

        ///<summary>Finds the class that the string represents within any loaded assembly. Calling with "MyNameSpace.MyObject" would return the same type as typeof(MyNameSpace.MyObject) etc.</summary>
        public static bool TryGetType(this string valueType, [MaybeNullWhen(false)]out Type type)
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
                                      // ReSharper disable once RedundantEnumerableCastCall
                                     .Cast<Type>()
                                     .ToArray();
                if (types.None())
                {
                    return false;
                }

                if (types.Length > 1)
                {
                    throw new MultipleMatchingTypesException(valueType);
                }

                type = types.Single();
                TypeMap.Add(valueType, types.Single());
                return true;
            }
        }

        public static IEnumerable<Type> ClassInheritanceChain(this Type me)
        {
            Type? current = me;
            while(current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public static string GetFullNameCompilable(this Type @this)
        {
            if(!@this.IsConstructedGenericType) return @this.FullName!.ReplaceInvariant("+", ".");

            var typeArguments = @this.GenericTypeArguments;
            // ReSharper disable once PossibleNullReferenceException
            var genericTypeName = @this.GetGenericTypeDefinition().GetFullNameCompilable().ReplaceInvariant($@"`{typeArguments.Length}", "");

            var name = $"{genericTypeName}<{typeArguments.Select(type => type.GetFullNameCompilable()).Join(",")}>";

            return name;
        }

        ///<summary>Thrown if there is more than one type that matches the string passed to <see cref="TypeCE.AsType"/></summary>
        public class MultipleMatchingTypesException : Exception
        {
            internal MultipleMatchingTypesException(string typeName): base(typeName)
            {
            }
        }

        ///<summary>Thrown if there is no type that matches the string passed to <see cref="TypeCE.AsType"/> is found</summary>
        public class FailedToFindTypeException : Exception
        {
            internal FailedToFindTypeException(string typeName): base(typeName)
            {
            }
        }
    }
}