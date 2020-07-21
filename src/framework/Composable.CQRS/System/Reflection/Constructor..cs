using System;
using System.Collections.Generic;
using System.Reflection;

namespace Composable.System.Reflection
{
    static partial class Constructor
    {
        internal static class For<TInstance>
        {
            internal static class DefaultConstructor
            {
                internal static readonly Func<TInstance> Instance = Compile.ForReturnType<TInstance>().DefaultConstructor();
            }

            internal static class WithArgument<TArgument1>
            {
                internal static readonly Func<TArgument1, TInstance> Instance = Compile.ForReturnType<TInstance>().WithArgument<TArgument1>();
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

        internal static object CreateInstance(Type type) => Activator.CreateInstance(type, nonPublic: true);//This is highly optimized nowadays. Compiling a constructor wins only when we don't need to do even a lookup by type.
    }
}
