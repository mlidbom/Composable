using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Composable.System.Reflection
{
    ///<summary>Constructs instances of classes</summary>
    public static class ObjectFactory<TEntity>
    {
        private interface ITypedArgument
        {
            Type Type { get; }
            object Argument { get; }
        }

        private struct TypedArgument<TParameter> : ITypedArgument
        {
            public TypedArgument(TParameter argument) { Argument = argument; }

            public Type Type => typeof(TParameter);
            public object Argument { get; }
        }

        ///<summary>Creates an instance of TEntity using a constructor matching the specified argument types</summary>
        public static TEntity CreateInstance<TParameter1>(TParameter1 argument1)
        {
            return CreateInstanceInternal(new TypedArgument<TParameter1>(argument1));
        }

        ///<summary>Creates an instance of TEntity using a constructor matching the specified argument types</summary>
        public static TEntity CreateInstance<TParameter1, TParameter2>(TParameter1 argument1, TParameter2 argument2)
        {
            return CreateInstanceInternal(
                new TypedArgument<TParameter1>(argument1),
                new TypedArgument<TParameter2>(argument2));
        }

        ///<summary>Creates an instance of TEntity using a constructor matching the specified argument types</summary>
        public static TEntity CreateInstance<TParameter1, TParameter2, TParameter3>(TParameter1 argument1, TParameter2 argument2, TParameter3 argument3)
        {
            return CreateInstanceInternal(
                new TypedArgument<TParameter1>(argument1),
                new TypedArgument<TParameter2>(argument2),
                new TypedArgument<TParameter3>(argument3));
        }

        ///<summary>Creates an instance of TEntity using a constructor matching the specified argument types</summary>
        public static TEntity CreateInstance<TParameter1, TParameter2, TParameter3, TParameter4>(TParameter1 argument1, TParameter2 argument2, TParameter3 argument3, TParameter4 argument4)
        {
            return CreateInstanceInternal(
                new TypedArgument<TParameter1>(argument1),
                new TypedArgument<TParameter2>(argument2),
                new TypedArgument<TParameter3>(argument3),
                new TypedArgument<TParameter4>(argument4));
        }

        private static TEntity CreateInstanceInternal(params ITypedArgument[] typedArguments)
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


        //private static TEntity CreateInstanceInternalV2(params ITypedArgument[] typedArguments)
        //{
        //    var argumentsArray = typedArguments.Select(typedArgument => typedArgument.Argument).ToArray();
        //    var parameterTypes = typedArguments.Select(typedArgument => typedArgument.Type).ToArray();
        //    ParameterModifier[] parameterModifiers = new ParameterModifier[0];

        //    var bindingFlags = BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public;
        //    var entityType = typeof(TEntity);

        //    var constructor = entityType.GetConstructor(
        //        bindingAttr: bindingFlags,
        //        binder: null,
        //        callConvention: CallingConventions.Standard,
        //        types: parameterTypes,
        //        modifiers: parameterModifiers);

        //    return (TEntity)constructor.Invoke(invokeAttr: bindingFlags, binder: null, obj: null, parameters: argumentsArray, culture: null);
        //}


        private static string DescribeParameterList(IEnumerable<Type> parameterTypes)
        {
            return parameterTypes.Select(parameterType => parameterType.FullName).Join(", ");
        }
    }
}
