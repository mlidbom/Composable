using System;
using Composable.System.Threading;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.System.Threading
{
    [Serializable]
    class SharedObject
    {
        public string Name { get; set; } = "Default";
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
