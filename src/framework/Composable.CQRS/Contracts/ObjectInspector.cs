

// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable UnusedMember.Global

namespace Composable.Contracts
{
    ///<summary>Performs inspections on objects</summary>
    public static class ObjectInspector
    {
        /// <summary>
        /// <para>Throws <see cref="ObjectIsNullContractViolationException"/> if any inspected value is null</para>
        /// <para>Consider using <see cref="NotNullOrDefault{TArgument}"/> instead as it works for value types as well and is only marginally slower.</para>
        /// </summary>
        public static IInspected<TValue> NotNull<TValue>(this IInspected<TValue> me)
            where TValue : class
        {
            return me.Inspect(
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse ReSharper incorrectly believes nullable reference types to deliver runtime guarantees.
                inspected => !(inspected is null),
                badValue => new ObjectIsNullContractViolationException(badValue));
        }


        /// <summary>
        /// <para>Throws <see cref="ObjectIsDefaultContractViolationException"/> if any inspected value is default(TValue). Such as 0 for integer, Guid.Empty for Guid, new MyStruct() for any struct.</para>
        /// <para>Consider using <see cref="NotNullOrDefault{TValue}"/> instead as it works for reference types as well and is only marginally slower.</para>
        /// </summary>
        internal static IInspected<TValue> NotDefault<TValue>(this IInspected<TValue> me)
            where TValue : struct
        {
            return me.Inspect(
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse ReSharper is plain about this warning.
                inspected => !Equals(inspected, default(TValue)),
                badValue => new ObjectIsDefaultContractViolationException(badValue));
        }


        /// <summary>
        /// <para>Throws <see cref="ObjectIsNullContractViolationException"/> if any inspected value is null</para>
        /// <para>Throws <see cref="ObjectIsDefaultContractViolationException"/> if any inspected value is default(TValue). Such as 0 for integer, Guid.Empty for Guid, new SomeStruct().</para>
        /// </summary>
        public static IInspected<TValue> NotNullOrDefault<TValue>(this IInspected<TValue> me)
        {
            me.Inspect(
                inspected => !(inspected is null),
                badValue => new ObjectIsNullContractViolationException(badValue));

            return me.Inspect(
                inspected => !NullOrDefaultTester<TValue>.IsNullOrDefault(inspected),
                badValue => new ObjectIsDefaultContractViolationException(badValue));
        }

        internal static IInspected<object> IsOfType<TRequiredType>(this IInspected<object> @this)
        {
            return @this.Inspect(value => value is TRequiredType,
                                 value => new ContractViolationException(value));
        }
    }
}
