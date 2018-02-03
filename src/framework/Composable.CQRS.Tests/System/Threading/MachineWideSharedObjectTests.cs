using Composable.Serialization;
using Composable.System.Threading;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.System.Threading
{
    class SharedObject : BinarySerializedObject<SharedObject>
    {
        static SharedObject()
        {
            Init(() => new SharedObject(),
                 GetterSetter.ForString(@this => @this.Name, (@this, value) => @this.Name = value));
        }

        public string Name { get; set; } = "Default";
    }

    [TestFixture]
    public class MachineWideSharedObjectTests
    {
        [Test] public void Create()
        {
            using (var shared = MachineWideSharedObject<SharedObject>.For("somethingPrettyUnique"))
            {
                var test = shared.GetCopy();

                test.Name.Should().Be("Default");
            }
        }

        [Test]
        public void Create_update_and_get()
        {
            using (var shared = MachineWideSharedObject<SharedObject>.For("somethingMoreUnique"))
            {
                var test = shared.GetCopy();

                test.Name.Should().Be("Default");

                test = shared.Update(@this => @this.Name = "Updated");

                test.Name.Should().Be("Updated");

                test = shared.GetCopy();

                test.Name.Should().Be("Updated");
            }
        }
    }
}
