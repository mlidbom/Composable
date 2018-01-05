using System;
using Composable.Contracts;

namespace Composable.Functional
{
    public static class Option
    {
        public static Option<T> NoneIfNull<T>(T value) where T: class => value == null ? None<T>() : Some(value);
        public static Option<T> NoneIfDefault<T>(T value) where T: struct => Equals(value, default(T)) ? None<T>() : Some(value);

        public static Option<T> Some<T>(T value) => new Some<T>(value);
        public static  Option<T> None<T>() => Functional.None<T>.Instance;
    }

    // ReSharper disable once UnusedTypeParameter It is used to maintain type safety in pattern matching. We need to know what the type of the Some value vill be even if the value is not included in this class.
    public abstract class Option<T>
    {
        [Obsolete("For the love of sanity do NOT add another inheritor to this class. It will break the whole abstraction!")]
        protected Option() {}

        public abstract bool HasValue { get; }
    }

#pragma warning disable 618
    public sealed class Some<T> : Option<T>
    {
        public Some(T value)
        {
            Contract.Argument.NotNull(value);
            Value = value;
        }

        public T Value { get; }
        public override bool HasValue => true;
    }

    public sealed class None<T> : Option<T>
    {
        None(){}
        internal static readonly None<T> Instance = new None<T>();
        public override bool HasValue => false;
    }
#pragma warning restore 618

}
