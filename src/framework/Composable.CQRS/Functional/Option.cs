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

    public abstract class Option<T> : DiscriminatedUnion<Option<T>, Some<T>, None<T>>
    {
        public abstract bool HasValue { get; }
    }

    public sealed class Some<T> : Option<T>
    {
        internal Some(T value)
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
}
