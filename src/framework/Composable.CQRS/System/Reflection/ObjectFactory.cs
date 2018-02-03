using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace Composable.System.Reflection
{
    ///<summary>Constructs instances of classes</summary>
    static class ObjectFactory<TEntity>
    {
        interface ITypedArgument
        {
            Type Type { get; }
            object Argument { get; }
        }

        struct TypedArgument<TParameter> : ITypedArgument
        {
            public TypedArgument(TParameter argument) => Argument = argument;

            public Type Type => typeof(TParameter);
            public object Argument { get; }
        }

        ///<summary>Creates an instance of TEntity using a constructor matching the specified argument types</summary>
        public static TEntity CreateInstance<TParameter1>(TParameter1 argument1) => CreateInstanceInternal(new TypedArgument<TParameter1>(argument1));

        static TEntity CreateInstanceInternal(params ITypedArgument[] typedArguments)
        {
            var argumentsArray = typedArguments.Select(typedArgument => typedArgument.Argument).ToArray();
            var parameterTypes = typedArguments.Select(typedArgument => typedArgument.Type).ToArray();

            try
            {
                return (TEntity)Activator.CreateInstance(
                    type: typeof(TEntity),
                    bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public,
                    binder: null,
                    args: argumentsArray,
                    culture: null);
            }
            catch(MissingMethodException exception)
            {
                throw new Exception(
                          $"Type:  must have a constructor, that can be private {typeof(TEntity).FullName}({DescribeParameterList(parameterTypes)})",
                          exception);
            }
        }


        static string DescribeParameterList(IEnumerable<Type> parameterTypes)
        {
            return parameterTypes.Select(parameterType => parameterType.FullName).Join(", ");
        }
    }

    static class ConstructorFor<TInstance>
    {
        internal static class DefaultConstructor
        {
            internal static readonly Func<TInstance> Instance = (Func<TInstance>)typeof(TInstance).ConstructorCompiler(typeof(Func< TInstance>));
        }

        internal static class WithArgumentTypes<TArgument1>
        {
            internal static readonly Func<TArgument1, TInstance> Instance = (Func<TArgument1, TInstance>)typeof(TInstance).ConstructorCompiler(typeof(Func<TArgument1, TInstance>));
        }

        internal static class WithArgumentTypes<TArgument1, TArgument2>
        {
            internal static readonly Func<TArgument1, TArgument2, TInstance> Instance = (Func<TArgument1, TArgument2, TInstance>)typeof(TInstance).ConstructorCompiler(typeof(Func<TArgument1, TArgument2, TInstance>));
        }

        internal static class WithArgumentTypes<TArgument1, TArgument2, TArgument3>
        {
            internal static readonly Func<TArgument1, TArgument2, TArgument3, TInstance> Instance = (Func<TArgument1, TArgument2, TArgument3, TInstance>)typeof(TInstance).ConstructorCompiler(typeof(Func<TArgument1, TArgument2, TArgument3, TInstance>));
        }

        internal static class WithArgumentTypes<TArgument1, TArgument2, TArgument3, TArgument4>
        {
            internal static readonly Func<TArgument1, TArgument2, TArgument3, TArgument4, TInstance> Instance = (Func<TArgument1, TArgument2, TArgument3, TArgument4, TInstance>)typeof(TInstance).ConstructorCompiler(typeof(Func<TArgument1, TArgument2, TArgument3, TArgument4, TInstance>));
        }

        internal static class WithArgumentTypes<TArgument1, TArgument2, TArgument3, TArgument4, TArgument5>
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
