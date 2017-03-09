using System;

namespace Composable.Contracts
{
    ///<summary>Performs inspections on objects</summary>
    public static class ObjectInspector
    {
        /// <summary>
        /// <para>Throws <see cref="ObjectIsNullContractViolationException"/> if any inspected value is null</para>
        /// <para>Consider using <see cref="NotNullOrDefault{TArgument}"/> instead as it works for value types as well and is only marginally slower.</para>
        /// </summary>
        public static Inspected<TValue> NotNull<TValue>(this Inspected<TValue> me)
            where TValue : class
        {
            return me.Inspect(
                inspected => !ReferenceEquals(inspected, null),
                badValue => new ObjectIsNullContractViolationException(badValue));
        }


        /// <summary>
        /// <para>Throws <see cref="ObjectIsDefaultContractViolationException"/> if any inspected value is default(TValue). Such as 0 for integer, Guid.Empty for Guid, new MyStruct() for any struct.</para>
        /// <para>Consider using <see cref="NotNullOrDefault{TValue}"/> instead as it works for reference types as well and is only marginally slower.</para>
        /// </summary>
        public static Inspected<TValue> NotDefault<TValue>(this Inspected<TValue> me)
            where TValue : struct
        {
            return me.Inspect(
                inspected => !Equals(inspected, default(TValue)),
                badValue => new ObjectIsDefaultContractViolationException(badValue));
        }


        /// <summary>
        /// <para>Throws <see cref="ObjectIsNullContractViolationException"/> if any inspected value is null</para>
        /// <para>Throws <see cref="ObjectIsDefaultContractViolationException"/> if any inspected value is default(TValue). Such as 0 for integer, Guid.Empty for Guid, new SomeStruct().</para>
        /// </summary>
        public static Inspected<TValue> NotNullOrDefault<TValue>(this Inspected<TValue> me)
        {
            me.Inspect(
                inspected => !ReferenceEquals(inspected, null),
                badValue => new ObjectIsNullContractViolationException(badValue));

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
                badValue => new ObjectIsDefaultContractViolationException(badValue));
        }

        public static Inspected<object> IsOfType<TRequiredType>(this Inspected<object> @this)
        {
            return @this.Inspect(value => value is TRequiredType,
                                 value => new ContractViolationException(value));
        }
    }
}
