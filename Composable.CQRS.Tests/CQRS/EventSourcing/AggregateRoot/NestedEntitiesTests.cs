using System;
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

            var entity1 = root.AddEntity("entity1");
            entity1.Name.Should().Be("entity1");
            root.Entities.InCreationOrder.Count.Should().Be(1);
            root.Entities.Exists(entity1.Id).Should().Be(true);
            root.Entities.Get(entity1.Id).Should().Be(entity1);
            root.Entities[entity1.Id].Should().Be(entity1);

            var entity2 = root.AddEntity("entity2");
            entity2.Name.Should().Be("entity2");
            root.Entities.InCreationOrder.Count.Should().Be(2);
            root.Entities.Exists(entity2.Id).Should().Be(true);
            root.Entities[entity2.Id].Should().Be(entity2);

            entity1.Rename("newName");
            entity1.Name.Should().Be("newName");
            entity2.Name.Should().Be("entity2");

            entity2.Rename("newName2");
            entity2.Name.Should().Be("newName2");
            entity1.Name.Should().Be("newName");

            root.Entities.InCreationOrder.Count.Should().Be(2);

            entity2.Remove();
            root.Entities.Exists(entity2.Id).Should().Be(false);
            root.Entities.InCreationOrder.Count.Should().Be(1);
            root.Invoking(_ => root.Entities.Get(entity2.Id)).ShouldThrow<Exception>();
            root.Invoking(_ => { var __ = root.Entities[entity2.Id]; }).ShouldThrow<Exception>();

            entity1.Remove();
            root.Entities.Exists(entity1.Id).Should().Be(false);
            root.Entities.InCreationOrder.Count.Should().Be(0);
            root.Invoking(_ => root.Entities.Get(entity1.Id)).ShouldThrow<Exception>();
            root.Invoking(_ => { var __ = root.Entities[entity1.Id]; }).ShouldThrow<Exception>();
        }

        [Test]
        public void ComponentPropertiesAreSetcorrectly() {
            var root = new Root("root");

            var component = root.Component;
            component.Name.Should().BeNullOrEmpty();

            component.Rename("newName");
            component.Name.Should().Be("newName");
        }

        [Test]
        public void EntityNestedInComponentWorks()
        {
            var root = new Root("root").Component;

            var entity1 = root.AddEntity("entity1");
            entity1.Name.Should().Be("entity1");
            root.Entities.InCreationOrder.Count.Should().Be(1);
            root.Entities.Exists(entity1.Id).Should().Be(true);
            root.Entities.Get(entity1.Id).Should().Be(entity1);
            root.Entities[entity1.Id].Should().Be(entity1);

            var entity2 = root.AddEntity("entity2");
            entity2.Name.Should().Be("entity2");
            root.Entities.InCreationOrder.Count.Should().Be(2);
            root.Entities.Exists(entity2.Id).Should().Be(true);
            root.Entities[entity2.Id].Should().Be(entity2);

            entity1.Rename("newName");
            entity1.Name.Should().Be("newName");
            entity2.Name.Should().Be("entity2");

            entity2.Rename("newName2");
            entity2.Name.Should().Be("newName2");
            entity1.Name.Should().Be("newName");

            root.Entities.InCreationOrder.Count.Should().Be(2);

            entity2.Remove();
            root.Entities.Exists(entity2.Id).Should().Be(false);
            root.Entities.InCreationOrder.Count.Should().Be(1);
            root.Invoking(_ => root.Entities.Get(entity2.Id)).ShouldThrow<Exception>();
            root.Invoking(_ => { var __ = root.Entities[entity2.Id]; }).ShouldThrow<Exception>();

            entity1.Remove();
            root.Entities.Exists(entity1.Id).Should().Be(false);
            root.Entities.InCreationOrder.Count.Should().Be(0);
            root.Invoking(_ => root.Entities.Get(entity1.Id)).ShouldThrow<Exception>();
            root.Invoking(_ => { var __ = root.Entities[entity1.Id]; }).ShouldThrow<Exception>();
        }


        [Test]
        public void EntityNestedInEntityWorks()
        {
            var root = new Root("root").AddEntity("RootEntityName");

            var entity1 = root.AddEntity("entity1");
            entity1.Name.Should().Be("entity1");
            root.Entities.InCreationOrder.Count.Should().Be(1);
            root.Entities.Exists(entity1.Id).Should().Be(true);
            root.Entities.Get(entity1.Id).Should().Be(entity1);
            root.Entities[entity1.Id].Should().Be(entity1);

            var entity2 = root.AddEntity("entity2");
            entity2.Name.Should().Be("entity2");
            root.Entities.InCreationOrder.Count.Should().Be(2);
            root.Entities.Exists(entity2.Id).Should().Be(true);
            root.Entities[entity2.Id].Should().Be(entity2);

            entity1.Rename("newName");
            entity1.Name.Should().Be("newName");
            entity2.Name.Should().Be("entity2");

            entity2.Rename("newName2");
            entity2.Name.Should().Be("newName2");
            entity1.Name.Should().Be("newName");

            root.Entities.InCreationOrder.Count.Should().Be(2);

            entity2.Remove();
            root.Entities.Exists(entity2.Id).Should().Be(false);
            root.Entities.InCreationOrder.Count.Should().Be(1);
            root.Invoking(_ => root.Entities.Get(entity2.Id)).ShouldThrow<Exception>();
            root.Invoking(_ => { var __ = root.Entities[entity2.Id]; }).ShouldThrow<Exception>();

            entity1.Remove();
            root.Entities.Exists(entity1.Id).Should().Be(false);
            root.Entities.InCreationOrder.Count.Should().Be(0);
            root.Invoking(_ => root.Entities.Get(entity1.Id)).ShouldThrow<Exception>();
            root.Invoking(_ => { var __ = root.Entities[entity1.Id]; }).ShouldThrow<Exception>();
        }

    }
}
