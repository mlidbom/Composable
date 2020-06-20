using System;
using System.Diagnostics.CodeAnalysis;

namespace Composable.Contracts {
    static class NullOrDefaultTester<TType>
    {
        static readonly Func<TType, bool> IsNullOrDefaultInternal;
        static NullOrDefaultTester()
        {
            var type = typeof(TType);

            if(type.IsInterface || type == typeof(object))
            {
                IsNullOrDefaultInternal = obj => (obj is null) || (obj.GetType().IsValueType && Equals(obj, Activator.CreateInstance(obj.GetType())));
                return;
            }

            if(type.IsClass)
            {
                IsNullOrDefaultInternal = obj => obj is null;
                return;
            }

            if(type.IsValueType)
            {
                var defaultValue = Activator.CreateInstance(type);
                IsNullOrDefaultInternal = obj => Equals(obj, defaultValue);
                return;
            }

            throw new Exception("WTF");
        }

        public static bool IsNullOrDefault([AllowNull]TType obj) => IsNullOrDefaultInternal(obj!);//We know that the method we are calling will correctly handle any null values but cannot declare it as such because it is a generic Func
    }
}