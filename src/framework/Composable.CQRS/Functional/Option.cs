using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.System;
using Composable.System.Linq;

namespace Composable.Functional
{
    public static class Option
    {
        public static Option<T> NoneIfNull<T>(T value) where T: class => value == null ? None<T>() : Some(value);
        public static Option<T> NoneIfDefault<T>(T value) where T: struct => Equals(value, default(T)) ? None<T>() : Some(value);

        public static Option<T> Some<T>(T value) => new Option<T>.Some(value);
        public static  Option<T> None<T>() => Option<T>.None.Instance;
    }

    public abstract class Option<T> : DiscriminatedUnion<Option<T>, Option<T>.Some, Option<T>.None>
    {
        Option() {}

        public abstract bool HasValue { get; }

        public sealed class Some : Option<T>
        {
            internal Some(T value)
            {
                Contract.Argument.NotNull(value);
                Value = value;
            }

            public T Value { get; }
            public override bool HasValue => true;
        }

        public sealed class None : Option<T>
        {
            None(){}
            internal static readonly None Instance = new None();
            public override bool HasValue => false;
        }
    }
}
