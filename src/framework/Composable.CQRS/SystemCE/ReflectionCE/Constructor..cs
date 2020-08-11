using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Composable.Contracts;
using Composable.GenericAbstractions;

namespace Composable.SystemCE.ReflectionCE
{
    static partial class Constructor
    {
        internal static class For<TInstance>
        {
            internal static class DefaultConstructor
            {
                internal static readonly Func<TInstance> Instance = CreateInstanceFactory();
                static Func<TInstance> CreateInstanceFactory() =>
                    typeof(TInstance).Is<IStaticInstancePropertySingleton>()
                        ? CompileStaticInstancePropertyDelegate()
                        : Compile.ForReturnType<TInstance>().DefaultConstructor();

                static Func<TInstance> CompileStaticInstancePropertyDelegate()
                {
                    PropertyInfo instanceProperty = typeof(TInstance).GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                                                                     .SingleOrDefault(prop => prop.Name == "Instance" && prop.PropertyType == typeof(TInstance))
                                                 ?? throw new Exception($"{nameof(IStaticInstancePropertySingleton)} implementation: {typeof(TInstance).GetFullNameCompilable()} does not have a public property named Instance of of the same type.");

                    return Expression.Lambda<Func<TInstance>>(Expression.Property(null, instanceProperty)).Compile();
                }
            }

            internal static class WithArguments<TArgument1>
            {
                internal static readonly Func<TArgument1, TInstance> Instance = Compile.ForReturnType<TInstance>().WithArguments<TArgument1>();
            }

            internal static class WithArguments<TArgument1, TArgument2>
            {
                internal static readonly Func<TArgument1, TArgument2, TInstance> Instance = Compile.ForReturnType<TInstance>().WithArguments<TArgument1, TArgument2>();
            }

            internal static class WithArguments<TArgument1, TArgument2, TArgument3>
            {
                internal static readonly Func<TArgument1, TArgument2, TArgument3, TInstance> Instance = Compile.ForReturnType<TInstance>().WithArguments<TArgument1, TArgument2, TArgument3>();
            }

            internal static class WithArguments<TArgument1, TArgument2, TArgument3, TArgument4>
            {
                internal static readonly Func<TArgument1, TArgument2, TArgument3, TArgument4, TInstance> Instance = Compile.ForReturnType<TInstance>().WithArguments<TArgument1, TArgument2, TArgument3, TArgument4>();
            }

            internal static class WithArguments<TArgument1, TArgument2, TArgument3, TArgument4, TArgument5>
            {
                internal static readonly Func<TArgument1, TArgument2, TArgument3, TArgument4, TArgument5, TInstance> Instance = Compile.ForReturnType<TInstance>().WithArguments<TArgument1, TArgument2, TArgument3, TArgument4, TArgument5>();
            }
        }

        internal static bool HasDefaultConstructor(Type type) => type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) != null;

        internal static object CreateInstance(Type type) => Contract.ReturnNotNull(Activator.CreateInstance(type, nonPublic: true)); //This is highly optimized nowadays. Compiling a constructor wins only when we don't need to do even a lookup by type.
    }
}
