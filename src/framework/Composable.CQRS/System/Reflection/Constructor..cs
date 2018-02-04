using System;
using System.Collections.Generic;

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
                var newConstructor = Compile.ForReturnType<object>().WithImplementingType(type).DefaultConstructor();
                newConstructors.Add(type, newConstructor);
                _defaultConstructors = newConstructors;
                return newConstructor;
            }
        }
    }
}
