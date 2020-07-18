using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System;
using Composable.System.Linq;

namespace Composable.Functional
{
    static class DistriminatedUnion
    {
        internal static void AssertValidType(object instance, IReadOnlyList<Type> allowedTypes)
        {
            var instanceType = instance.GetType();
            if(!allowedTypes.Contains(instanceType))
            {
                throw new InvalidDiscriminatedUnionTypeException(instanceType, allowedTypes);
            }
        }

        public class InvalidDiscriminatedUnionTypeException : Exception
        {
            public InvalidDiscriminatedUnionTypeException(Type instanceType, IReadOnlyList<Type> allowedTypes) : base($"{instanceType.FullName} is not one of {allowedTypes.Select(type => type.FullName).Join(",")}") {}
        }
    }

    public abstract class DiscriminatedUnion<TInheritor, TOption1, TOption2>
        where TInheritor : DiscriminatedUnion<TInheritor, TOption1, TOption2>
        where TOption1 : TInheritor
        where TOption2 : TInheritor
    {
        static readonly IReadOnlyList<Type> AllowedTypes = Seq.OfTypes<TOption1, TOption2>().ToList();
        protected DiscriminatedUnion() => DistriminatedUnion.AssertValidType(this, AllowedTypes);
    }

    public abstract class DiscriminatedUnion<TInheritor, TOption1, TOption2, TOption3>
        where TInheritor : DiscriminatedUnion<TInheritor, TOption1, TOption2, TOption3>
        where TOption1 : TInheritor
        where TOption2 : TInheritor
        where TOption3 : TInheritor
    {
        static readonly IReadOnlyList<Type> AllowedTypes = Seq.OfTypes<TOption1, TOption2, TOption3>().ToList();
        protected DiscriminatedUnion() => DistriminatedUnion.AssertValidType(this, AllowedTypes);
    }

    public abstract class DiscriminatedUnion<TInheritor, TOption1, TOption2, TOption3, TOption4>
        where TInheritor : DiscriminatedUnion<TInheritor, TOption1, TOption2, TOption3, TOption4>
        where TOption1 : TInheritor
        where TOption2 : TInheritor
        where TOption3 : TInheritor
        where TOption4 : TInheritor
    {
        static readonly IReadOnlyList<Type> AllowedTypes = Seq.OfTypes<TOption1, TOption2, TOption3, TOption4>().ToList();
        protected DiscriminatedUnion() => DistriminatedUnion.AssertValidType(this, AllowedTypes);
    }

    public abstract class DiscriminatedUnion<TInheritor, TOption1, TOption2, TOption3, TOption4, TOption5>
        where TInheritor : DiscriminatedUnion<TInheritor, TOption1, TOption2, TOption3, TOption4, TOption5>
        where TOption1 : TInheritor
        where TOption2 : TInheritor
        where TOption3 : TInheritor
        where TOption4 : TInheritor
        where TOption5 : TInheritor
    {
        static readonly IReadOnlyList<Type> AllowedTypes = Seq.OfTypes<TOption1, TOption2, TOption3, TOption4, TOption5>().ToList();
        protected DiscriminatedUnion() => DistriminatedUnion.AssertValidType(this, AllowedTypes);
    }

    public abstract class DiscriminatedUnion<TInheritor, TOption1, TOption2, TOption3, TOption4, TOption5, TOption6>
        where TInheritor : DiscriminatedUnion<TInheritor, TOption1, TOption2, TOption3, TOption4, TOption5, TOption6>
        where TOption1 : TInheritor
        where TOption2 : TInheritor
        where TOption3 : TInheritor
        where TOption4 : TInheritor
        where TOption5 : TInheritor
        where TOption6 : TInheritor
    {
        static readonly IReadOnlyList<Type> AllowedTypes = Seq.OfTypes<TOption1, TOption2, TOption3, TOption4, TOption5>().ToList();
        protected DiscriminatedUnion() => DistriminatedUnion.AssertValidType(this, AllowedTypes);
    }
}
