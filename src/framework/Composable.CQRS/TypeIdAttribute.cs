using System;
using Composable.Contracts;
using Composable.DDD;

namespace Composable
{
    class TypeId : ValueObject<TypeId>
    {
        internal readonly Guid GuidValue;

        // ReSharper disable once ImpureMethodCallOnReadonlyValueField
        public override string ToString() => GuidValue.ToString();

        internal TypeId(Guid guidValue)
        {
            Contract.Argument.Assert(guidValue != Guid.Empty);
            GuidValue = guidValue;
        }
    }
}
