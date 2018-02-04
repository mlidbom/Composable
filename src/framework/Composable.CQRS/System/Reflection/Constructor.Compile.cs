using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Composable.System.Reflection
{
    static partial class Constructor
    {
        internal static class Compile
        {
            internal static CompilerBuilder<TTypeToConstruct> ForReturnType<TTypeToConstruct>() => new CompilerBuilder<TTypeToConstruct>();
            internal static CompilerBuilder<object> ForReturnType(Type typeToConstruct) => new CompilerBuilder<object>(typeToConstruct);


            internal class CompilerBuilder<TInstance>
            {
                readonly Type _typeToConstruct;
                internal CompilerBuilder(Type typeToConstruct) => _typeToConstruct = typeToConstruct;

                internal CompilerBuilder() : this(typeof(TInstance))
                {
                }

                internal CompilerBuilder<TInstance> WithImplementingType(Type actualType)
                {
                    if(!typeof(TInstance).IsAssignableFrom(actualType))
                    {
                        throw new Exception("Impossible type combination.");
                    }
                    return new CompilerBuilder<TInstance>(actualType);
                }

                internal Delegate WithArgumentTypes(Type argument1Type) => CompileForSignature(typeof(Func<,>).MakeGenericType(argument1Type, _typeToConstruct));
                internal Delegate WithArgumentTypes(Type argument1Type, Type argument2Type) => CompileForSignature(typeof(Func<,,>).MakeGenericType(argument1Type, argument2Type, _typeToConstruct));
                internal Delegate WithArgumentTypes(Type argument1Type, Type argument2Type, Type argument3Type) => CompileForSignature(typeof(Func<,,,>).MakeGenericType(argument1Type, argument2Type, argument3Type, _typeToConstruct));
                internal Delegate WithArgumentTypes(Type argument1Type, Type argument2Type, Type argument3Type, Type argument4Type) => CompileForSignature(typeof(Func<,,,>).MakeGenericType(argument1Type, argument2Type, argument3Type, argument4Type, _typeToConstruct));
                internal Delegate WithArgumentTypes(Type argument1Type, Type argument2Type, Type argument3Type, Type argument4Type, Type argument5Type) => CompileForSignature(typeof(Func<,,,,>).MakeGenericType(argument1Type, argument2Type, argument3Type, argument4Type, argument5Type, _typeToConstruct));

                internal Func<TInstance> DefaultConstructor() => (Func<TInstance>)CompileForSignature(typeof(Func<>).MakeGenericType(_typeToConstruct));
                internal Func<TArgument1, TInstance> WithArgument<TArgument1>() => (Func<TArgument1, TInstance>)WithArgumentTypes(typeof(TArgument1));
                internal Func<TArgument1, TArgument2, TInstance> WithArguments<TArgument1, TArgument2>() => (Func<TArgument1, TArgument2, TInstance>)WithArgumentTypes(typeof(TArgument1), typeof(TArgument2));
                internal Func<TArgument1, TArgument2, TArgument3, TInstance> WithArguments<TArgument1, TArgument2, TArgument3>() => (Func<TArgument1, TArgument2, TArgument3, TInstance>)WithArgumentTypes(typeof(TArgument1), typeof(TArgument2), typeof(TArgument3));
                internal Func<TArgument1, TArgument2, TArgument3, TArgument4, TInstance> WithArguments<TArgument1, TArgument2, TArgument3, TArgument4>() => (Func<TArgument1, TArgument2, TArgument3, TArgument4, TInstance>)WithArgumentTypes(typeof(TArgument1), typeof(TArgument2), typeof(TArgument3), typeof(TArgument4));
                internal Func<TArgument1, TArgument2, TArgument3, TArgument4, TArgument5, TInstance> WithArguments<TArgument1, TArgument2, TArgument3, TArgument4, TArgument5>() => (Func<TArgument1, TArgument2, TArgument3, TArgument4, TArgument5, TInstance>)WithArgumentTypes(typeof(TArgument1), typeof(TArgument2), typeof(TArgument3), typeof(TArgument4), typeof(TArgument5));

                static Delegate CompileForSignature(Type delegateType)
                {
                    var delegateTypeGenericArgumentTypes = delegateType.GetGenericArguments();
                    var instanceType = delegateTypeGenericArgumentTypes[delegateTypeGenericArgumentTypes.Length -1];
                    var constructorArgumentTypes = delegateTypeGenericArgumentTypes.Length > 1 ? delegateTypeGenericArgumentTypes.Take(delegateTypeGenericArgumentTypes.Length - 1).ToArray() : Type.EmptyTypes;

                    ConstructorInfo constructor = instanceType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, binder: null, types: constructorArgumentTypes, modifiers: null);
                    if (constructor == null)
                    {
                        throw new Exception($"Expected to find a constructor with the signature: [private|protected|public] {instanceType.FullName}({DescribeParameterList(constructorArgumentTypes)})");
                    }

                    var constructorCallMethod = new DynamicMethod($"Generated_constructor_for_{instanceType.Name}", instanceType, constructorArgumentTypes, instanceType);
                    var ilGenerator = constructorCallMethod.GetILGenerator();
                    for (var argumentIndex = 0; argumentIndex < constructorArgumentTypes.Length; argumentIndex++)
                    {
                        ilGenerator.Emit(OpCodes.Ldarg, argumentIndex);
                    }
                    ilGenerator.Emit(OpCodes.Newobj, constructor);
                    ilGenerator.Emit(OpCodes.Ret);
                    return constructorCallMethod.CreateDelegate(delegateType);
                }

                static string DescribeParameterList(IEnumerable<Type> parameterTypes)
                {
                    return parameterTypes.Select(parameterType => parameterType.FullName).Join(", ");
                }
            }
        }
    }
}
