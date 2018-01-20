using System;
using Composable.Contracts;
using Composable.DDD;

namespace Composable
{
    class TypeId : ValueObject<TypeId>
    {
        internal readonly Guid GuidValue;
        internal readonly Guid ParentTypeGuidValue;

        public override string ToString() => $"{GuidValue}:{ParentTypeGuidValue}";

        internal TypeId(Guid guidValue, Guid parentTypeGuidValue)
        {
            Contract.Argument.Assert(guidValue != Guid.Empty);
            GuidValue = guidValue;
            ParentTypeGuidValue = parentTypeGuidValue;
        }

        public static TypeId Parse(string eventTypeId)
        {
            var primaryGuid = eventTypeId.Substring(0, 36);
            var secondaryGuid = eventTypeId.Substring(37, 36);

            return new TypeId(Guid.Parse(primaryGuid), Guid.Parse(secondaryGuid));
        }
    }
}
