using System;

namespace Composable.Contracts
{
    public static class ObjectInspector
    {
        /// <summary>
        /// <para>Throws <see cref="ObjectIsNullException"/> if any inspected value is null</para>
        /// <para>Consider using <see cref="NotNullOrDefault{TArgument}"/> instead as it works for value types as well and is only marginally slower.</para>
        /// </summary>
        public static Inspected<TValue> NotNull<TValue>(this Inspected<TValue> me)
            where TValue : class
        {
            return me.Inspect(
                inspected => !ReferenceEquals(inspected, null),
                badValue => new ObjectIsNullException(badValue.Name));
        }


        /// <summary>
        /// <para>Throws <see cref="ObjectIsDefaultException"/> if any inspected value is default(TValue). Such as 0 for integer, Guid.Empty for Guid, new MyStruct() for any struct.</para>
        /// <para>Consider using <see cref="NotNullOrDefault{TValue}"/> instead as it works for reference types as well and is only marginally slower.</para>
        /// </summary>
        public static Inspected<TValue> NotDefault<TValue>(this Inspected<TValue> me)
            where TValue : struct
        {
            return me.Inspect(
                inspected => !Equals(inspected, Activator.CreateInstance(inspected.GetType())),
                badValue => new ObjectIsDefaultException(badValue.Name));
        }


        /// <summary>
        /// <para>Throws <see cref="ObjectIsNullException"/> if any inspected value is null</para>
        /// <para>Throws <see cref="ObjectIsDefaultException"/> if any inspected value is default(TValue). Such as 0 for integer, Guid.Empty for Guid, new SomeStruct().</para>
        /// </summary>
        public static Inspected<TValue> NotNullOrDefault<TValue>(this Inspected<TValue> me)
        {
            me.Inspect(
                inspected => !ReferenceEquals(inspected, null),
                badValue => new ObjectIsNullException(badValue.Name));

            return me.Inspect(
                inspected =>
                {
                    if(!inspected.GetType().IsValueType)
                    {
                        return true;
                    }
                    var valueTypeDefault = Activator.CreateInstance(inspected.GetType());
                    return !Equals(inspected, valueTypeDefault);
                },
                badValue => new ObjectIsDefaultException(badValue.Name));
        }
    }
}
