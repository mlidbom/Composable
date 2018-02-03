using System;
using System.Collections.Generic;
using Composable.Serialization;
using FluentAssertions;
using NUnit.Framework;

// ReSharper disable BuiltInTypeReferenceStyle
// ReSharper disable MemberCanBePrivate.Local we want the inspection of the objects to include all properties...
// ReSharper disable MemberCanBePrivate.Global

namespace Composable.Tests.Serialization.BinarySerializeds
{
    [TestFixture] public class BinarySerializedTests
    {
        class SingleStringProperty : BinarySerialized<SingleStringProperty>
        {
            public string Name { get; set; } = "Default";
            protected override IEnumerable<MemberGetterSetter> CreateGetterSetters() => new[] {GetterSetter.ForString(instance => instance.Name, (instance, value) => instance.Name = value)};
        }

        [Test] public void Class_with_single_property_roundtrips_correctly()
        {
            var singleString = new SingleStringProperty() {Name = "NonDefault"};

            var data = singleString.Serialize();
            var roundTripped = SingleStringProperty.Deserialize(data);

            roundTripped.ShouldBeEquivalentTo(singleString);
        }

        [Test] public void Instance_with_all_property_types_and_null_recursive_property_list_and_array_roundtrip_correctly()
        {
            var allPropertyTypesCountingFrom1 = new HasAllPropertyTypes(true, 2, 'a', new decimal(3.2), 4.1, 5, 6, 7, 8, 9, 10, 11.1f, 12, "13", Guid.Parse("00000000-0000-0000-0000-000000000014"), DateTime.FromBinary(15));


            var data = allPropertyTypesCountingFrom1.Serialize();
            var roundTripped = HasAllPropertyTypes.Deserialize(data);

            roundTripped.ShouldBeEquivalentTo(allPropertyTypesCountingFrom1);
        }

        [Test] public void Instance_with_recursive_property_with_all_value_type_roundtrip_correctly()
        {
            var allPropertyTypesCountingFrom1 = new HasAllPropertyTypes(true, 2, 'a', new decimal(3.2), 4.1, 5, 6, 7, 8, 9, 10, 11.1f, 12, "13", Guid.Parse("00000000-0000-0000-0000-000000000014"), DateTime.FromBinary(15))
                                                {
                                                    RecursiveProperty = new HasAllPropertyTypes(true, 2, 'a', new decimal(3.2), 4.1, 5, 6, 7, 8, 9, 10, 11.1f, 12, "13", Guid.Parse("00000000-0000-0000-0000-000000000014"), DateTime.FromBinary(15))
                                                };


            var data = allPropertyTypesCountingFrom1.Serialize();
            var roundTripped = HasAllPropertyTypes.Deserialize(data);

            roundTripped.ShouldBeEquivalentTo(allPropertyTypesCountingFrom1);
        }

        [Test] public void Instance_with_recursive_list_property_with_one_null_value_roundtrip_correctly()
        {
            var allPropertyTypesCountingFrom1 = HasAllPropertyTypes.CreateInstance();

            allPropertyTypesCountingFrom1.RecursiveListProperty = new List<HasAllPropertyTypes>()
                                                                  {
                                                                      HasAllPropertyTypes.CreateInstance(),
                                                                      null,
                                                                      HasAllPropertyTypes.CreateInstance()
                                                                  };


            var data = allPropertyTypesCountingFrom1.Serialize();
            var roundTripped = HasAllPropertyTypes.Deserialize(data);

            roundTripped.ShouldBeEquivalentTo(allPropertyTypesCountingFrom1);
        }


        [Test] public void Instance_with_recursive_array_property_with_one_null_value_roundtrip_correctly()
        {
            var allPropertyTypesCountingFrom1 = HasAllPropertyTypes.CreateInstance();

            allPropertyTypesCountingFrom1.RecursiveArrayProperty = new[]
                                                                  {
                                                                      HasAllPropertyTypes.CreateInstance(),
                                                                      null,
                                                                      HasAllPropertyTypes.CreateInstance()
                                                                  };


            var data = allPropertyTypesCountingFrom1.Serialize();
            var roundTripped = HasAllPropertyTypes.Deserialize(data);

            roundTripped.ShouldBeEquivalentTo(allPropertyTypesCountingFrom1);
        }
    }
}
