using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Composable.System.Reflection
{
    static class Constructor
    {
        internal static class For<TInstance>
        {
            internal static class DefaultConstructor
            {
                internal static readonly Func<TInstance> Instance = (Func<TInstance>)CompileForSignature(typeof(Func< TInstance>));
            }

            internal static class WithArgument<TArgument1>
            {
                internal static readonly Func<TArgument1, TInstance> Instance = (Func<TArgument1, TInstance>)CompileForSignature(typeof(Func<TArgument1, TInstance>));
            }

            internal static class WithArguments<TArgument1, TArgument2>
            {
                internal static readonly Func<TArgument1, TArgument2, TInstance> Instance = (Func<TArgument1, TArgument2, TInstance>)CompileForSignature(typeof(Func<TArgument1, TArgument2, TInstance>));
            }

            internal static class WithArguments<TArgument1, TArgument2, TArgument3>
            {
                internal static readonly Func<TArgument1, TArgument2, TArgument3, TInstance> Instance = (Func<TArgument1, TArgument2, TArgument3, TInstance>)CompileForSignature(typeof(Func<TArgument1, TArgument2, TArgument3, TInstance>));
            }

            internal static class WithArguments<TArgument1, TArgument2, TArgument3, TArgument4>
            {
                internal static readonly Func<TArgument1, TArgument2, TArgument3, TArgument4, TInstance> Instance = (Func<TArgument1, TArgument2, TArgument3, TArgument4, TInstance>)CompileForSignature(typeof(Func<TArgument1, TArgument2, TArgument3, TArgument4, TInstance>));
            }

            internal static class WithArguments<TArgument1, TArgument2, TArgument3, TArgument4, TArgument5>
            {
                internal static readonly Func<TArgument1, TArgument2, TArgument3, TArgument4, TArgument5, TInstance> Instance = (Func<TArgument1, TArgument2, TArgument3, TArgument4, TArgument5, TInstance>)CompileForSignature(typeof(Func<TArgument1, TArgument2, TArgument3, TArgument4, TArgument5, TInstance>));
            }

        }

        internal static object CreateInstance(Type type) => DefaultFor(type)();

        static Dictionary<Type, Func<object>> _defaultConstructors = new Dictionary<Type, Func<object>>();
        internal static Func<object> DefaultFor(Type type)
        {
            if(_defaultConstructors.TryGetValue(type, out var constructor))
            {
                return constructor;
            }

            lock(_defaultConstructors)
            {
                var newConstructors = new Dictionary<Type, Func<object>>(_defaultConstructors);
                var newConstructor = (Func<object>)CompileForSignature(typeof(Func<>).MakeGenericType(type));
                newConstructors.Add(type, newConstructor);
                _defaultConstructors = newConstructors;
                return newConstructor;
            }
        }

        internal static Delegate CompileForSignature(Type delegateType)
        {
            var delegateTypeGenericArgumentTypes = delegateType.GetGenericArguments();
            var instanceType = delegateTypeGenericArgumentTypes[delegateTypeGenericArgumentTypes.Length -1];
            var constructorArgumentTypes = delegateTypeGenericArgumentTypes.Length > 1 ? delegateTypeGenericArgumentTypes.Take(delegateTypeGenericArgumentTypes.Length - 1).ToArray() : Type.EmptyTypes;

            ConstructorInfo constructor = instanceType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, binder: null, types: constructorArgumentTypes, modifiers: null);
            if (constructor == null)
            {
                if (constructorArgumentTypes.Length == 0)
                {
                    throw new InvalidProgramException(string.Format("Type '{0}' doesn't have a parameterless constructor.", instanceType.Name));
                }
                throw new InvalidProgramException(string.Format("Type '{0}' doesn't have the requested constructor.", instanceType.Name));
            }

            var dynamicMethod = new DynamicMethod("DM$_" + instanceType.Name, instanceType, constructorArgumentTypes, instanceType);
            var ilGen = dynamicMethod.GetILGenerator();
            for (var i = 0; i < constructorArgumentTypes.Length; i++)
            {
                ilGen.Emit(OpCodes.Ldarg, i);
            }
            ilGen.Emit(OpCodes.Newobj, constructor);
            ilGen.Emit(OpCodes.Ret);
            return dynamicMethod.CreateDelegate(delegateType);
        }

    }
}
