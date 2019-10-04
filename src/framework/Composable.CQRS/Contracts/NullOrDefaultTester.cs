using System;

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

        public static bool IsNullOrDefault(TType obj) => IsNullOrDefaultInternal(obj);
    }
}