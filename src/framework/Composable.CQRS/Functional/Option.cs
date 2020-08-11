using System.Diagnostics.CodeAnalysis;
using Composable.Contracts;
using Composable.SystemCE;

namespace Composable.Functional
{
    public static class Option
    {
        public static Option<T> NoneIfNull<T>(T? value) where T: class => value == null ? None<T>() : Some(value);
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse ReSharper is plain about this warning.
        public static Option<T> NoneIfDefault<T>(T value) where T: struct => Equals(value, default(T)) ? None<T>() : Some(value);

        public static Option<T> Some<T>(T value) => new Some<T>(value);
        public static  Option<T> None<T>() => Functional.None<T>.Instance;
        public static bool IsSome<T>(Option<T> option) => option is Some<T>;
    }

    public abstract class Option<T> : DiscriminatedUnion<Option<T>, Some<T>, None<T>>
    {
        public abstract bool HasValue { get; }
    }

    public sealed class Some<T> : Option<T>
    {
        internal Some(T value)
        {
            Assert.Argument.NotNull(value);
            Value = value;
        }

        [NotNull]public T Value { get; }
        public override bool HasValue => true;
    }

    public sealed class None<T> : Option<T>, IStaticInstancePropertySingleton
    {
        None(){}
        internal static readonly None<T> Instance = new None<T>();
        public override bool HasValue => false;
    }
}
