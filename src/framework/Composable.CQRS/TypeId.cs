using System;
using Composable.Contracts;
using Newtonsoft.Json;

// ReSharper disable ImpureMethodCallOnReadonlyValueField

namespace Composable
{
    class TypeId
    {
        [JsonProperty]internal readonly Guid GuidValue;

        // ReSharper disable once ImpureMethodCallOnReadonlyValueField
        public override string ToString() => GuidValue.ToString();

        [JsonConstructor]internal TypeId(Guid guidValue)
        {
            Assert.Argument.Assert(guidValue != Guid.Empty);
            GuidValue = guidValue;
        }

        public override bool Equals(object other) => other is TypeId otherTypeId && otherTypeId.GuidValue.Equals(GuidValue);
        public override int GetHashCode() => GuidValue.GetHashCode();

        public static bool operator ==(TypeId left, TypeId right) => Equals(left, right);
        public static bool operator !=(TypeId left, TypeId right) => !Equals(left, right);
    }
}
