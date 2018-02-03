using System.Collections.Generic;
using Composable.Serialization;
using Composable.System.Threading;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.System.Threading
{
    class SharedObject : BinarySerialized<SharedObject>
    {
        public string Name { get; set; } = "Default";
        protected override IEnumerable<MemberGetterSetter> CreateGetterSetters() => new[] {GetterSetter.ForString(@this => @this.Name, (@this, value) => @this.Name = value)};
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
