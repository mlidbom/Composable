using System;
using Composable.Persistence.EventStore;
using Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.GuidId.Domain;
using Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.GuidId.Domain.Events;
using Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.GuidId.QueryModels;
using FluentAssertions;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

// ReSharper disable MemberHidesStaticFromOuterClass
namespace Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.GuidId
{
    [TestFixture]
    public class NestedEntitiesTests
    {
        Root Ag;
        RootQueryModel Qm;

        [SetUp]public void Setup()
        {
            Ag = new Root("root");
            Qm = new RootQueryModel();
            var eventStored = ((IEventStored)Ag);
            eventStored.EventStream.Subscribe(@event => Qm.ApplyEvent((RootEvent.IRoot)@event));
            Qm.LoadFromHistory(eventStored.GetChanges());
        }

        [Test] public void ConstructorWorks()
        {
            Ag.Name.Should().Be("root");
            Qm.Name.Should().Be("root");
        }

        [Test]
        public void Createing_nested_entities_works_and_events_dispatch_correctly()
        {
            var agEntity1 = Ag.AddEntity("entity1");
            var qmEntity1 = Qm.Entities.InCreationOrder[0];
            agEntity1.Name.Should().Be("entity1");
            qmEntity1.Name.Should().Be("entity1");
            Ag.Entities.InCreationOrder.Count.Should().Be(1);
            Qm.Entities.InCreationOrder.Count.Should().Be(1);
            Ag.Entities.Exists(agEntity1.Id).Should().Be(true);
            Qm.Entities.Exists(qmEntity1.Id).Should().Be(true);
            Ag.Entities.Get(agEntity1.Id).Should().Be(agEntity1);
            Qm.Entities.Get(qmEntity1.Id).Should().Be(qmEntity1);
            Ag.Entities[agEntity1.Id].Should().Be(agEntity1);
            Qm.Entities[qmEntity1.Id].Should().Be(qmEntity1);

            var agEntity2 = Ag.AddEntity("entity2");
            var qmEntity2 = Qm.Entities.InCreationOrder[1];
            agEntity2.Name.Should().Be("entity2");
            qmEntity2.Name.Should().Be("entity2");
            Ag.Entities.InCreationOrder.Count.Should().Be(2);
            Qm.Entities.InCreationOrder.Count.Should().Be(2);
            Ag.Entities.Exists(agEntity2.Id).Should().Be(true);
            Qm.Entities.Exists(qmEntity2.Id).Should().Be(true);
            Ag.Entities[agEntity2.Id].Should().Be(agEntity2);
            Qm.Entities[qmEntity2.Id].Should().Be(qmEntity2);

            agEntity1.Rename("newName");
            agEntity1.Name.Should().Be("newName");
            qmEntity1.Name.Should().Be("newName");
            agEntity2.Name.Should().Be("entity2");
            qmEntity2.Name.Should().Be("entity2");

            agEntity2.Rename("newName2");
            agEntity2.Name.Should().Be("newName2");
            qmEntity2.Name.Should().Be("newName2");
            agEntity1.Name.Should().Be("newName");
            qmEntity1.Name.Should().Be("newName");


            Ag.Entities.InCreationOrder.Count.Should().Be(2);
            Qm.Entities.InCreationOrder.Count.Should().Be(2);

            agEntity2.Remove();
            Ag.Entities.Exists(agEntity2.Id).Should().Be(false);
            Qm.Entities.Exists(qmEntity2.Id).Should().Be(false);
            Ag.Entities.InCreationOrder.Count.Should().Be(1);
            Qm.Entities.InCreationOrder.Count.Should().Be(1);
            Ag.Invoking(_ => Ag.Entities.Get(agEntity2.Id)).ShouldThrow<Exception>();
            Qm.Invoking(_ => Ag.Entities.Get(qmEntity2.Id)).ShouldThrow<Exception>();
            Ag.Invoking(_ => { var __ = Ag.Entities[agEntity2.Id]; }).ShouldThrow<Exception>();
            Qm.Invoking(_ => { var __ = Ag.Entities[qmEntity2.Id]; }).ShouldThrow<Exception>();

            agEntity1.Remove();
            Ag.Entities.Exists(agEntity1.Id).Should().Be(false);
            Qm.Entities.Exists(agEntity1.Id).Should().Be(false);
            Ag.Entities.InCreationOrder.Count.Should().Be(0);
            Qm.Entities.InCreationOrder.Count.Should().Be(0);
            Ag.Invoking(_ => Ag.Entities.Get(agEntity1.Id)).ShouldThrow<Exception>();
            Qm.Invoking(_ => Ag.Entities.Get(agEntity1.Id)).ShouldThrow<Exception>();
            Ag.Invoking(_ => { var __ = Ag.Entities[agEntity1.Id]; }).ShouldThrow<Exception>();
            Qm.Invoking(_ => { var __ = Ag.Entities[agEntity1.Id]; }).ShouldThrow<Exception>();
        }

        [Test]
        public void ComponentPropertiesAreSetcorrectly()
        {
            var agComponent = Ag.Component;
            var qmComponent = Qm.Component;
            agComponent.Name.Should().BeNullOrEmpty();
            qmComponent.Name.Should().BeNullOrEmpty();

            agComponent.Rename("newName");
            agComponent.Name.Should().Be("newName");
            qmComponent.Name.Should().Be("newName");
        }

        [Test]
        public void EntityNestedInComponentWorks()
        {
            var agEntity1 = Ag.AddEntity("entity1");
            var qmEntity1 = Qm.Entities.InCreationOrder[0];
            qmEntity1.Id.Should().Be(agEntity1.Id);
            agEntity1.Name.Should().Be("entity1");
            qmEntity1.Name.Should().Be("entity1");
            Ag.Entities.InCreationOrder.Count.Should().Be(1);
            Qm.Entities.InCreationOrder.Count.Should().Be(1);
            Ag.Entities.Exists(agEntity1.Id).Should().Be(true);
            Qm.Entities.Exists(agEntity1.Id).Should().Be(true);
            Ag.Entities.Get(agEntity1.Id).Should().Be(agEntity1);
            Qm.Entities.Get(agEntity1.Id).Should().Be(qmEntity1);
            Ag.Entities[agEntity1.Id].Should().Be(agEntity1);
            Qm.Entities[agEntity1.Id].Should().Be(qmEntity1);

            var agEntity2 = Ag.AddEntity("entity2");
            var qmEntity2 = Qm.Entities.InCreationOrder[1];
            agEntity2.Name.Should().Be("entity2");
            qmEntity2.Name.Should().Be("entity2");
            Ag.Entities.InCreationOrder.Count.Should().Be(2);
            Qm.Entities.InCreationOrder.Count.Should().Be(2);
            Ag.Entities.Exists(agEntity2.Id).Should().Be(true);
            Qm.Entities.Exists(agEntity2.Id).Should().Be(true);
            Ag.Entities[agEntity2.Id].Should().Be(agEntity2);
            Qm.Entities[agEntity2.Id].Should().Be(qmEntity2);

            agEntity1.Rename("newName");
            agEntity1.Name.Should().Be("newName");
            qmEntity1.Name.Should().Be("newName");
            agEntity2.Name.Should().Be("entity2");
            qmEntity2.Name.Should().Be("entity2");

            agEntity2.Rename("newName2");
            agEntity2.Name.Should().Be("newName2");
            qmEntity2.Name.Should().Be("newName2");
            agEntity1.Name.Should().Be("newName");
            qmEntity1.Name.Should().Be("newName");

            Ag.Entities.InCreationOrder.Count.Should().Be(2);
            Qm.Entities.InCreationOrder.Count.Should().Be(2);

            agEntity2.Remove();
            Ag.Entities.Exists(agEntity2.Id).Should().Be(false);
            Qm.Entities.Exists(agEntity2.Id).Should().Be(false);
            Ag.Entities.InCreationOrder.Count.Should().Be(1);
            Qm.Entities.InCreationOrder.Count.Should().Be(1);
            Ag.Invoking(_ => Ag.Entities.Get(agEntity2.Id)).ShouldThrow<Exception>();
            Qm.Invoking(_ => Ag.Entities.Get(agEntity2.Id)).ShouldThrow<Exception>();
            Ag.Invoking(_ => { var __ = Ag.Entities[agEntity2.Id]; }).ShouldThrow<Exception>();
            Qm.Invoking(_ => { var __ = Ag.Entities[agEntity2.Id]; }).ShouldThrow<Exception>();

            agEntity1.Remove();
            Ag.Entities.Exists(agEntity1.Id).Should().Be(false);
            Qm.Entities.Exists(agEntity1.Id).Should().Be(false);
            Ag.Entities.InCreationOrder.Count.Should().Be(0);
            Qm.Entities.InCreationOrder.Count.Should().Be(0);
            Ag.Invoking(_ => Ag.Entities.Get(agEntity1.Id)).ShouldThrow<Exception>();
            Qm.Invoking(_ => Ag.Entities.Get(agEntity1.Id)).ShouldThrow<Exception>();
            Ag.Invoking(_ => { var __ = Ag.Entities[agEntity1.Id]; }).ShouldThrow<Exception>();
            Qm.Invoking(_ => { var __ = Ag.Entities[agEntity1.Id]; }).ShouldThrow<Exception>();
        }


        [Test]
        public void EntityNestedInEntityWorks()
        {
            var agRootEntity = Ag.AddEntity("RootEntityName");

            var entity1 = agRootEntity.AddEntity("entity1");
            entity1.Name.Should().Be("entity1");
            agRootEntity.Entities.InCreationOrder.Count.Should().Be(1);
            agRootEntity.Entities.Exists(entity1.Id).Should().Be(true);
            agRootEntity.Entities.Get(entity1.Id).Should().Be(entity1);
            agRootEntity.Entities[entity1.Id].Should().Be(entity1);

            var entity2 = agRootEntity.AddEntity("entity2");
            entity2.Name.Should().Be("entity2");
            agRootEntity.Entities.InCreationOrder.Count.Should().Be(2);
            agRootEntity.Entities.Exists(entity2.Id).Should().Be(true);
            agRootEntity.Entities[entity2.Id].Should().Be(entity2);

            entity1.Rename("newName");
            entity1.Name.Should().Be("newName");
            entity2.Name.Should().Be("entity2");

            entity2.Rename("newName2");
            entity2.Name.Should().Be("newName2");
            entity1.Name.Should().Be("newName");

            agRootEntity.Entities.InCreationOrder.Count.Should().Be(2);

            entity2.Remove();
            agRootEntity.Entities.Exists(entity2.Id).Should().Be(false);
            agRootEntity.Entities.InCreationOrder.Count.Should().Be(1);
            agRootEntity.Invoking(_ => agRootEntity.Entities.Get(entity2.Id)).ShouldThrow<Exception>();
            agRootEntity.Invoking(_ => { var __ = agRootEntity.Entities[entity2.Id]; }).ShouldThrow<Exception>();

            entity1.Remove();
            agRootEntity.Entities.Exists(entity1.Id).Should().Be(false);
            agRootEntity.Entities.InCreationOrder.Count.Should().Be(0);
            agRootEntity.Invoking(_ => agRootEntity.Entities.Get(entity1.Id)).ShouldThrow<Exception>();
            agRootEntity.Invoking(_ => { var __ = agRootEntity.Entities[entity1.Id]; }).ShouldThrow<Exception>();
        }

    }
}
