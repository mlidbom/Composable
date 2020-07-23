using System;
using Composable.Persistence.EventStore;
using Composable.SystemCE.ReactiveCE;
using Composable.Tests.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain;
using Composable.Tests.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain.Events;
using Composable.Tests.CQRS.Aggregates.NestedEntitiesTests.GuidId.QueryModels;
using FluentAssertions;
using NUnit.Framework;
// ReSharper disable InconsistentNaming
// ReSharper disable ImplicitlyCapturedClosure

// ReSharper disable MemberHidesStaticFromOuterClass
namespace Composable.Tests.CQRS.Aggregates.NestedEntitiesTests.GuidId
{
    [TestFixture]
    public class NestedEntitiesTests
    {
        Root Ag;
        RootQueryModel Qm;
        Guid AggregateId;

        [SetUp]public void Setup()
        {
            AggregateId = Guid.NewGuid();
            Ag = new Root("root", AggregateId);
            Qm = new RootQueryModel();
            var eventStored = ((IEventStored)Ag);
            eventStored.EventStream.Subscribe(@event => Qm.ApplyEvent((RootEvent.IRoot)@event));
            Qm.LoadFromHistory(eventStored.GetChanges());
        }

        [Test] public void ConstructorWorks()
        {
            Ag.Name.Should().Be("root");
            Qm.Name.Should().Be("root");
            Ag.Id.Should().Be(AggregateId);
            Qm.Id.Should().Be(AggregateId);
        }

        [Test]
        public void Aggregate_entity_tests()
        {
            var agEntity1 = Ag.AddEntity("entity1");
            var qmEntity1 = Qm.Entities.InCreationOrder[0];
            qmEntity1.Id.Should().Be(agEntity1.Id);
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
            Ag.Invoking(_ => Ag.Entities.Get(agEntity2.Id)).Should().Throw<Exception>();
            Qm.Invoking(_ => Ag.Entities.Get(qmEntity2.Id)).Should().Throw<Exception>();
            Ag.Invoking(_ => { var __ = Ag.Entities[agEntity2.Id]; }).Should().Throw<Exception>();
            Qm.Invoking(_ => { var __ = Ag.Entities[qmEntity2.Id]; }).Should().Throw<Exception>();

            agEntity1.Remove();
            Ag.Entities.Exists(agEntity1.Id).Should().Be(false);
            Qm.Entities.Exists(agEntity1.Id).Should().Be(false);
            Ag.Entities.InCreationOrder.Count.Should().Be(0);
            Qm.Entities.InCreationOrder.Count.Should().Be(0);
            Ag.Invoking(_ => Ag.Entities.Get(agEntity1.Id)).Should().Throw<Exception>();
            Qm.Invoking(_ => Ag.Entities.Get(agEntity1.Id)).Should().Throw<Exception>();
            Ag.Invoking(_ => { var __ = Ag.Entities[agEntity1.Id]; }).Should().Throw<Exception>();
            Qm.Invoking(_ => { var __ = Ag.Entities[agEntity1.Id]; }).Should().Throw<Exception>();
        }

        [Test]
        public void Aggregate_Component_tests()
        {
            Ag.Component.Name.Should().BeNullOrEmpty();
            Qm.Component.Name.Should().BeNullOrEmpty();

            Ag.Component.Rename("newName");
            Ag.Component.Name.Should().Be("newName");
            Qm.Component.Name.Should().Be("newName");
        }

        [Test]
        public void Aggregate_Component_Component_tests()
        {
            Ag.Component.CComponent.Name.Should().BeNullOrEmpty();
            Qm.Component.CComponent.Name.Should().BeNullOrEmpty();
            Ag.Component.CComponent.Rename("newName");
            Ag.Component.CComponent.Name.Should().Be("newName");
            Qm.Component.CComponent.Name.Should().Be("newName");
        }

        [Test]
        public void Aggregate_Component_Entity_tests()
        {
            var agComponent = Ag.Component;
            var qmComponent = Qm.Component;

            var entity1Id = Guid.NewGuid();
            var agComponentEntity1 = agComponent.AddEntity("entity1", entity1Id);
            agComponent.Invoking(@this => @this.AddEntity("entity2", entity1Id)).Should().Throw<Exception>();

            var qmComponentEntity1 = qmComponent.Entities.InCreationOrder[0];

            qmComponentEntity1.Id.Should().Be(agComponentEntity1.Id).And.Be(entity1Id);
            agComponentEntity1.Name.Should().Be("entity1");
            qmComponentEntity1.Name.Should().Be("entity1");
            agComponent.Entities.InCreationOrder.Count.Should().Be(1);
            qmComponent.Entities.InCreationOrder.Count.Should().Be(1);
            agComponent.Entities.Exists(agComponentEntity1.Id).Should().Be(true);
            qmComponent.Entities.Exists(agComponentEntity1.Id).Should().Be(true);
            agComponent.Entities.Get(agComponentEntity1.Id).Should().Be(agComponentEntity1);
            qmComponent.Entities.Get(agComponentEntity1.Id).Should().Be(qmComponentEntity1);
            agComponent.Entities[agComponentEntity1.Id].Should().Be(agComponentEntity1);
            qmComponent.Entities[agComponentEntity1.Id].Should().Be(qmComponentEntity1);

            var entity2Id = Guid.NewGuid();
            var agEntity2 = agComponent.AddEntity("entity2", entity2Id);
            agComponent.Invoking(@this => @this.AddEntity("entity3", entity2Id)).Should().Throw<Exception>();

            var qmEntity2 = qmComponent.Entities.InCreationOrder[1];
            agEntity2.Name.Should().Be("entity2");
            qmEntity2.Name.Should().Be("entity2");
            agComponent.Entities.InCreationOrder.Count.Should().Be(2);
            qmComponent.Entities.InCreationOrder.Count.Should().Be(2);
            agComponent.Entities.Exists(agEntity2.Id).Should().Be(true);
            qmComponent.Entities.Exists(agEntity2.Id).Should().Be(true);
            agComponent.Entities[agEntity2.Id].Should().Be(agEntity2);
            qmComponent.Entities[agEntity2.Id].Should().Be(qmEntity2);

            agComponentEntity1.Rename("newName");
            agComponentEntity1.Name.Should().Be("newName");
            qmComponentEntity1.Name.Should().Be("newName");
            agEntity2.Name.Should().Be("entity2");
            qmEntity2.Name.Should().Be("entity2");

            agEntity2.Rename("newName2");
            agEntity2.Name.Should().Be("newName2");
            qmEntity2.Name.Should().Be("newName2");
            agComponentEntity1.Name.Should().Be("newName");
            qmComponentEntity1.Name.Should().Be("newName");

            agComponent.Entities.InCreationOrder.Count.Should().Be(2);
            qmComponent.Entities.InCreationOrder.Count.Should().Be(2);

            agEntity2.Remove();
            agComponent.Entities.Exists(agEntity2.Id).Should().Be(false);
            qmComponent.Entities.Exists(agEntity2.Id).Should().Be(false);
            agComponent.Entities.InCreationOrder.Count.Should().Be(1);
            qmComponent.Entities.InCreationOrder.Count.Should().Be(1);
            agComponent.Invoking(@this => @this.Entities.Get(agEntity2.Id)).Should().Throw<Exception>();
            qmComponent.Invoking(@this => @this.Entities.Get(agEntity2.Id)).Should().Throw<Exception>();
            agComponent.Invoking(@this => { var __ = @this.Entities[agEntity2.Id]; }).Should().Throw<Exception>();
            qmComponent.Invoking(@this => { var __ = @this.Entities[agEntity2.Id]; }).Should().Throw<Exception>();

            agComponentEntity1.Remove();
            agComponent.Entities.Exists(agComponentEntity1.Id).Should().Be(false);
            qmComponent.Entities.Exists(agComponentEntity1.Id).Should().Be(false);
            agComponent.Entities.InCreationOrder.Count.Should().Be(0);
            qmComponent.Entities.InCreationOrder.Count.Should().Be(0);
            agComponent.Invoking(@this => @this.Entities.Get(agComponentEntity1.Id)).Should().Throw<Exception>();
            qmComponent.Invoking(@this => @this.Entities.Get(agComponentEntity1.Id)).Should().Throw<Exception>();
            agComponent.Invoking(@this => { var __ = @this.Entities[agComponentEntity1.Id]; }).Should().Throw<Exception>();
            qmComponent.Invoking(@this => { var __ = @this.Entities[agComponentEntity1.Id]; }).Should().Throw<Exception>();
        }


        [Test]
        public void Aggregate_Entity_Entity_tests()
        {
            var agRootEntity = Ag.AddEntity("RootEntityName");
            var qmRootEntity = Qm.Entities.InCreationOrder[0];

            var entity1Id = Guid.NewGuid();
            var agNestedEntity1 = agRootEntity.AddEntity("entity1", entity1Id);
            var qmNestedEntity1 = qmRootEntity.Entities.InCreationOrder[0];

            agRootEntity.Invoking(@this => @this.AddEntity("entity2", entity1Id)).Should().Throw<Exception>();

            agNestedEntity1.Id.Should().Be(entity1Id);
            qmNestedEntity1.Id.Should().Be(entity1Id);
            agNestedEntity1.Name.Should().Be("entity1");
            qmNestedEntity1.Name.Should().Be("entity1");
            agRootEntity.Entities.InCreationOrder.Count.Should().Be(1);
            qmRootEntity.Entities.InCreationOrder.Count.Should().Be(1);
            agRootEntity.Entities.Exists(agNestedEntity1.Id).Should().Be(true);
            qmRootEntity.Entities.Exists(agNestedEntity1.Id).Should().Be(true);
            agRootEntity.Entities.Get(agNestedEntity1.Id).Should().Be(agNestedEntity1);
            qmRootEntity.Entities.Get(agNestedEntity1.Id).Should().Be(qmNestedEntity1);
            agRootEntity.Entities[agNestedEntity1.Id].Should().Be(agNestedEntity1);
            qmRootEntity.Entities[agNestedEntity1.Id].Should().Be(qmNestedEntity1);

            var entity2Id = Guid.NewGuid();
            var agNestedEntity2 = agRootEntity.AddEntity("entity2", entity2Id);
            var qmNestedEntity2 = qmRootEntity.Entities.InCreationOrder[1];
            agRootEntity.Invoking(@this => @this.AddEntity("entity3", entity2Id)).Should().Throw<Exception>();

            agNestedEntity2.Id.Should().Be(entity2Id);
            qmNestedEntity2.Id.Should().Be(entity2Id);
            agNestedEntity2.Name.Should().Be("entity2");
            qmNestedEntity2.Name.Should().Be("entity2");
            agRootEntity.Entities.InCreationOrder.Count.Should().Be(2);
            qmRootEntity.Entities.InCreationOrder.Count.Should().Be(2);
            agRootEntity.Entities.Exists(agNestedEntity2.Id).Should().Be(true);
            qmRootEntity.Entities.Exists(agNestedEntity2.Id).Should().Be(true);
            agRootEntity.Entities[agNestedEntity2.Id].Should().Be(agNestedEntity2);
            qmRootEntity.Entities[agNestedEntity2.Id].Should().Be(qmNestedEntity2);

            agNestedEntity1.Rename("newName");
            agNestedEntity1.Name.Should().Be("newName");
            qmNestedEntity1.Name.Should().Be("newName");
            agNestedEntity2.Name.Should().Be("entity2");
            qmNestedEntity2.Name.Should().Be("entity2");

            agNestedEntity2.Rename("newName2");
            agNestedEntity2.Name.Should().Be("newName2");
            qmNestedEntity2.Name.Should().Be("newName2");
            agNestedEntity1.Name.Should().Be("newName");
            qmNestedEntity1.Name.Should().Be("newName");

            agRootEntity.Entities.InCreationOrder.Count.Should().Be(2);
            qmRootEntity.Entities.InCreationOrder.Count.Should().Be(2);

            agNestedEntity2.Remove();
            agRootEntity.Entities.Exists(agNestedEntity2.Id).Should().Be(false);
            qmRootEntity.Entities.Exists(agNestedEntity2.Id).Should().Be(false);
            agRootEntity.Entities.InCreationOrder.Count.Should().Be(1);
            qmRootEntity.Entities.InCreationOrder.Count.Should().Be(1);
            agRootEntity.Invoking(_ => agRootEntity.Entities.Get(agNestedEntity2.Id)).Should().Throw<Exception>();
            qmRootEntity.Invoking(_ => agRootEntity.Entities.Get(agNestedEntity2.Id)).Should().Throw<Exception>();
            agRootEntity.Invoking(_ => { var __ = agRootEntity.Entities[agNestedEntity2.Id]; }).Should().Throw<Exception>();
            qmRootEntity.Invoking(_ => { var __ = agRootEntity.Entities[agNestedEntity2.Id]; }).Should().Throw<Exception>();

            agNestedEntity1.Remove();
            agRootEntity.Entities.Exists(agNestedEntity1.Id).Should().Be(false);
            qmRootEntity.Entities.Exists(agNestedEntity1.Id).Should().Be(false);
            agRootEntity.Entities.InCreationOrder.Count.Should().Be(0);
            qmRootEntity.Entities.InCreationOrder.Count.Should().Be(0);
            agRootEntity.Invoking(_ => agRootEntity.Entities.Get(agNestedEntity1.Id)).Should().Throw<Exception>();
            qmRootEntity.Invoking(_ => agRootEntity.Entities.Get(agNestedEntity1.Id)).Should().Throw<Exception>();
            agRootEntity.Invoking(_ => { var __ = agRootEntity.Entities[agNestedEntity1.Id]; }).Should().Throw<Exception>();
            qmRootEntity.Invoking(_ => { var __ = agRootEntity.Entities[agNestedEntity1.Id]; }).Should().Throw<Exception>();
        }

    }
}
