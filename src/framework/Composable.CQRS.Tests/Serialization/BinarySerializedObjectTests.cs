using System;
using System.IO;
using Composable.Serialization;
using Composable.System.Threading;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.Serialization
{
    [TestFixture]public class BinarySerializedObjectTests
    {
        class SinglePropertyManualImplementation : IBinarySerializeMySelf
        {
            public string Name { get; set; } = "Default";
            public void Deserialize(BinaryReader reader) { Name = reader.ReadString(); }
            public void Serialize(BinaryWriter writer) { writer.Write(Name);}
        }

        class SingleStringProperty : BinarySerializedObject<SingleStringProperty>
        {
            static SingleStringProperty()
            {
                InitGetterSetters(GetterSetterFor.String(instance => instance.Name, (instance, value) => instance.Name = value));
            }

            public string Name { get; set; } = "Default";
        }

        [Test] public void Class_with_single_property_roundtrips_correctly()
        {
            var singleString = new SingleStringProperty() {Name = "NonDefault"};

            var data = singleString.Serialize();
            var roundTripped = SingleStringProperty.Deserialize(data);

            roundTripped.ShouldBeEquivalentTo(singleString);
        }
    }
}
