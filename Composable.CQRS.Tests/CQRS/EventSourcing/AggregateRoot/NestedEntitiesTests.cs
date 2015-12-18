using System;
using Composable.CQRS.EventSourcing;
using Composable.GenericAbstractions.Time;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

// ReSharper disable MemberHidesStaticFromOuterClass
namespace CQRS.Tests.CQRS.EventSourcing.AggregateRoot
{
    [TestFixture]
    public class NestedEntitiesTests
    {
        [Test]
        public void ConstructorWorks() {
            var root = new Root();
        }
    }

    public class Root : AggregateRoot<Root, RootEvent.Implementation.Root, RootEvent.IRoot>
    {
        public L1Entity.Collection L1Entities { get; }

        public Root() : base(new DateTimeNowTimeSource())
        {
            L1Entities = L1Entity.CreateSelfManagingCollection(this);

            RegisterEventAppliers().IgnoreUnhandled<RootEvent.IRoot>();

            RaiseEvent(new RootEvent.Implementation.Created(Guid.NewGuid()));
        }

        public L1Entity AddL1() { return L1Entities.Add(new RootEvent.L1Entity.Implementation.Created(Guid.NewGuid())); }
    }

    public class L1Entity : Root.NestedEntity<L1Entity, RootEvent.L1Entity.Implementation.Root, RootEvent.L1Entity.IRoot, RootEvent.L1Entity.Created> {}


    public static class RootEvent
    {
        public interface IRoot : IAggregateRootEvent {}

        public interface Created : IRoot, IAggregateRootCreatedEvent {}

        public static class Implementation
        {
            public abstract class Root : AggregateRootEvent, IRoot
            {
                protected Root() { }
                protected Root(Guid aggregateRootId) : base(aggregateRootId) { }
            }

            public class Created : Root, RootEvent.Created
            {
                public Created(Guid id) : base(id) { }
            }
        }

        public static class L1Entity
        {
            public interface IRoot : IAggregateRootComponentEvent, RootEvent.IRoot {}

            public interface Created : IRoot, IAggregateRootComponentCreatedEvent {}

            public static class Implementation
            {
                public abstract class Root : AggregateRoot.RootEvent.Implementation.Root, L1Entity.IRoot
                {
                    protected Root(Guid componentId) { ComponentId = componentId; }
                    public Guid ComponentId { get; }
                }

                public class Created : Root, L1Entity.Created
                {
                    public Created(Guid componentId) : base(componentId) { }
                }
            }
        }
    }
}
