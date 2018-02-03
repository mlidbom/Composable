using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Composable.System.Reflection
{
    static class ConstructorFor<TInstance>
    {
        internal static class DefaultConstructor
        {
            internal static readonly Func<TInstance> Instance = (Func<TInstance>)typeof(TInstance).ConstructorCompiler(typeof(Func< TInstance>));
        }

        internal static class WithArgument<TArgument1>
        {
            internal static readonly Func<TArgument1, TInstance> CreateIntance = (Func<TArgument1, TInstance>)typeof(TInstance).ConstructorCompiler(typeof(Func<TArgument1, TInstance>));
        }

        internal static class WithArguments<TArgument1, TArgument2>
        {
            internal static readonly Func<TArgument1, TArgument2, TInstance> Instance = (Func<TArgument1, TArgument2, TInstance>)typeof(TInstance).ConstructorCompiler(typeof(Func<TArgument1, TArgument2, TInstance>));
        }

        internal static class WithArguments<TArgument1, TArgument2, TArgument3>
        {
            internal static readonly Func<TArgument1, TArgument2, TArgument3, TInstance> Instance = (Func<TArgument1, TArgument2, TArgument3, TInstance>)typeof(TInstance).ConstructorCompiler(typeof(Func<TArgument1, TArgument2, TArgument3, TInstance>));
        }

        internal static class WithArguments<TArgument1, TArgument2, TArgument3, TArgument4>
        {
            internal static readonly Func<TArgument1, TArgument2, TArgument3, TArgument4, TInstance> Instance = (Func<TArgument1, TArgument2, TArgument3, TArgument4, TInstance>)typeof(TInstance).ConstructorCompiler(typeof(Func<TArgument1, TArgument2, TArgument3, TArgument4, TInstance>));
        }

        internal static class WithArguments<TArgument1, TArgument2, TArgument3, TArgument4, TArgument5>
        {
            internal static readonly Func<TArgument1, TArgument2, TArgument3, TArgument4, TArgument5, TInstance> Instance = (Func<TArgument1, TArgument2, TArgument3, TArgument4, TArgument5, TInstance>)typeof(TInstance).ConstructorCompiler(typeof(Func<TArgument1, TArgument2, TArgument3, TArgument4, TArgument5, TInstance>));
        }
    }

    static class Something
    {
        public static Delegate ConstructorCompiler(this Type type, Type delegateType)
        {
            var delegateTypeGenericArgumentTypes = delegateType.GetGenericArguments();
            var constructorArgumentTypes = delegateTypeGenericArgumentTypes.Length > 1 ? delegateTypeGenericArgumentTypes.Take(delegateTypeGenericArgumentTypes.Length - 1).ToArray() : Type.EmptyTypes;

            ConstructorInfo constructor = type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, binder: null, types: constructorArgumentTypes, modifiers: null);
            if (constructor == null)
            {
                if (constructorArgumentTypes.Length == 0)
                {
                    throw new InvalidProgramException(string.Format("Type '{0}' doesn't have a parameterless constructor.", type.Name));
                }
                throw new InvalidProgramException(string.Format("Type '{0}' doesn't have the requested constructor.", type.Name));
            }

            var dynamicMethod = new DynamicMethod("DM$_" + type.Name, type, constructorArgumentTypes, type);
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
