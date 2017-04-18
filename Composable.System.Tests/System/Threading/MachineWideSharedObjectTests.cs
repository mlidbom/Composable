using System;
using System.IO;
using Composable.System.Threading;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.System.Threading
{
    [Serializable]
    class SharedObject : IBinarySerializeMySelf
    {
        public string Name { get; set; } = "Default";
        public void Deserialize(BinaryReader reader) { Name = reader.ReadString(); }
        public void Serialize(BinaryWriter writer) { writer.Write(Name);}
    }

    [TestFixture]
    public class MachineWideSharedObjectTests
    {
        [Test] public void Create()
        {
            var shared = MachineWideSharedObject<SharedObject>.For("somethingprettyunique");
            var test = shared.GetCopy();

            test.Name.Should()
                .Be("Default");
        }

        [Test]
        public void Create_update_and_get()
        {
            var shared = MachineWideSharedObject<SharedObject>.For("somethingmoreunique");
            var test = shared.GetCopy();

            test.Name.Should()
                .Be("Default");

            test = shared.Update(@this => @this.Name = "Updated");

            test.Name.Should()
                .Be("Updated");

            test = shared.GetCopy();

            test.Name.Should()
                .Be("Updated");
        }
    }
}
