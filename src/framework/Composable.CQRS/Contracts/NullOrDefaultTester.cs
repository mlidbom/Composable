using System;
using System.Diagnostics.CodeAnalysis;

namespace Composable.Contracts {
    static class NullOrDefaultTester<TType>
    {
        static readonly Func<TType, bool> IsNullOrDefaultInternal = CreateNullOrDefaultTester();
        static Func<TType, bool> CreateNullOrDefaultTester()
        {
            var type = typeof(TType);

            if(type.IsInterface || type == typeof(object))
            {
                return obj => (obj is null) || (obj.GetType().IsValueType && Equals(obj, Activator.CreateInstance(obj.GetType())));
            }

            if(type.IsClass)
            {
                return obj => obj is null;
            }

            if(type.IsValueType)
            {
                var defaultValue = Activator.CreateInstance(type);
                return obj => Equals(obj, defaultValue);
            }

            throw new Exception("WTF");
        }

        public static bool IsNullOrDefault([AllowNull]TType obj) => IsNullOrDefaultInternal(obj!);//We know that the method we are calling will correctly handle any null values but cannot declare it as such because it is a generic Func
    }
}