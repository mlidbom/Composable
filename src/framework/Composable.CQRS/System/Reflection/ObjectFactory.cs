using System;
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

    public static class DynamicModuleLambdaCompiler
    {
        public static Func<T> GenerateFactory<T>() where T:new()
        {
            Expression<Func<T>> expr = () => new T();
            NewExpression newExpr = (NewExpression)expr.Body;
 
            var method = new DynamicMethod(name: "lambda", returnType: newExpr.Type, parameterTypes: new Type[0], m: typeof(DynamicModuleLambdaCompiler).Module, skipVisibility: true);
 
            ILGenerator ilGen = method.GetILGenerator();
            // Constructor for value types could be null
            if (newExpr.Constructor != null)
            {
                ilGen.Emit(OpCodes.Newobj, newExpr.Constructor);
            }
            else
            {
                LocalBuilder temp = ilGen.DeclareLocal(newExpr.Type);
                ilGen.Emit(OpCodes.Ldloca, temp);
                ilGen.Emit(OpCodes.Initobj, newExpr.Type);
                ilGen.Emit(OpCodes.Ldloc, temp);
            }

            ilGen.Emit(OpCodes.Ret);
 
            return (Func<T>)method.CreateDelegate(typeof(Func<T>));
        }
    }
}
