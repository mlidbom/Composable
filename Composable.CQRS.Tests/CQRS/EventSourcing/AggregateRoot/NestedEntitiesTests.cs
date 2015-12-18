using System;
using Composable.CQRS.EventSourcing;
using Composable.GenericAbstractions.Time;
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
        public void Createing_nested_entities_works()
        {
            var root = new Root("root");
            var l1_1 = root.AddL1("l1_1");
            l1_1.Name.Should().Be("l1_1");

            root.L1Entities.Get(l1_1.Id).Should().Be(l1_1);

            var l1_2 = root.AddL1("l1_2");
            l1_2.Name.Should().Be("l1_2");
            root.L1Entities.Get(l1_2.Id).Should().Be(l1_2);
        }
    }

    public class Root : AggregateRoot<Root, RootEvent.Implementation.Root, RootEvent.IRoot>
    {
        public string Name { get; private set; }
        public L1Entity.Collection L1Entities { get; }

        public Root(string name) : base(new DateTimeNowTimeSource())
        {
            L1Entities = L1Entity.CreateSelfManagingCollection(this);

            RegisterEventAppliers()
                .For<RootEvent.PropertyUpdated.Name>(e => Name = e.Name);

            RaiseEvent(new RootEvent.Implementation.Created(Guid.NewGuid(), name));
        }

        public L1Entity AddL1(string name) { return L1Entities.Add(new RootEvent.L1Entity.Implementation.Created(Guid.NewGuid(), name)); }
    }

    public class L1Entity : Root.NestedEntity<L1Entity, RootEvent.L1Entity.Implementation.Root, RootEvent.L1Entity.IRoot, RootEvent.L1Entity.Created>
    {
        public string Name { get; private set; }
        public L1Entity()
        {
            RegisterEventAppliers()
                .For<RootEvent.L1Entity.PropertyUpdated.Name>(e => Name = e.Name);
        }
    }

    public static class RootEvent
    {
        public interface IRoot : IAggregateRootEvent {}

        public interface Created : IRoot, IAggregateRootCreatedEvent, PropertyUpdated.Name {}

        public static class PropertyUpdated
        {
            public interface Name : RootEvent.IRoot
            {
                string Name { get; }
            }
        }

        public static class Implementation
        {
            public abstract class Root : AggregateRootEvent, IRoot
            {
                protected Root() { }
                protected Root(Guid aggregateRootId) : base(aggregateRootId) { }
            }

            public class Created : Root, RootEvent.Created
            {
                public Created(Guid id, string name) : base(id) { Name = name; }
                public string Name { get; }
            }
        }

        public static class L1Entity
        {
            public interface IRoot : IAggregateRootComponentEvent, RootEvent.IRoot {}

            public interface Created : IRoot, IAggregateRootEntityCreatedEvent, PropertyUpdated.Name {}

            public static class PropertyUpdated
            {
                public interface Name : IRoot
                {
                    string Name { get; }
                }
            }

            public static class Implementation
            {
                public abstract class Root : RootEvent.Implementation.Root, L1Entity.IRoot
                {
                    protected Root(Guid entityId) { EntityId = entityId; }
                    public Guid EntityId { get; }
                }

                public class Created : Root, L1Entity.Created
                {
                    public Created(Guid entityId, string name) : base(entityId) { Name = name; }
                    public string Name { get; }
                }
            }
        }
    }
}
