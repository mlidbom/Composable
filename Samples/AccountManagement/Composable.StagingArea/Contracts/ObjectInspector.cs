using System;
using Composable.Contracts.Tests;

namespace Composable.Contracts
{
    public static class ObjectInspector
    {
        public static Inspected<TArgument> NotNull<TArgument>(this Inspected<TArgument> me)
            where TArgument : class
        {
            return me.Inspect(
                inspected => !ReferenceEquals(inspected, null),
                badValue => new ObjectIsNullException(badValue.Name));
        }

        public static Inspected<TArgument> NotDefault<TArgument>(this Inspected<TArgument> me)
            where TArgument : struct
        {
            return me.Inspect(
                inspected => !Equals(inspected, Activator.CreateInstance(inspected.GetType())),
                badValue => new ObjectIsDefaultException(badValue.Name));
        }

        public static Inspected<TArgument> NotNullOrDefault<TArgument>(this Inspected<TArgument> me)
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
