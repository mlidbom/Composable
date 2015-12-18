using FluentAssertions;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

// ReSharper disable MemberHidesStaticFromOuterClass
namespace CQRS.Tests.CQRS.EventSourcing.AggregateRoot
{
    [TestFixture]
    public class NestedEntitiesTests
    {
        [Test]
        public void ConstructorWorks() { new Root("root").Name.Should().Be("root"); }

        [Test]
        public void Createing_nested_entities_works_and_events_dispatch_correctly()
        {
            var root = new Root("root");
            var l1_1 = root.AddL1("l1_1");
            l1_1.Name.Should().Be("l1_1");

            root.L1Entities.Get(l1_1.Id).Should().Be(l1_1);

            var l1_2 = root.AddL1("l1_2");
            l1_2.Name.Should().Be("l1_2");
            root.L1Entities.Get(l1_2.Id).Should().Be(l1_2);

            l1_1.Rename("newName");
            l1_1.Name.Should().Be("newName");
            l1_2.Name.Should().Be("l1_2");

            l1_2.Rename("newName2");
            l1_2.Name.Should().Be("newName2");
            l1_1.Name.Should().Be("newName");
        }

        [Test]
        public void ComponentPropertiesAreSetcorrectly() {
            var root = new Root("root");

            var component = root.L1Component;
            component.Name.Should().BeNullOrEmpty();

            component.Rename("newName");
            component.Name.Should().Be("newName");
        }
    }
}
