using System;
using Composable.CQRS.EventSourcing;
using Composable.GenericAbstractions.Time;
using CQRS.Tests.CQRS.EventSourcing.AggregateRoot.NestedEntitiesTests.GuidId.Domain.Events;

namespace CQRS.Tests.CQRS.EventSourcing.AggregateRoot.NestedEntitiesTests.GuidId.Domain
{
    public class Root : AggregateRoot<Root, RootEvent.Implementation.Root, RootEvent.IRoot>
    {
        public string Name { get; private set; }
        private readonly Entity.CollectionManager _entities;
        public Component Component { get; private set; }

        public Root(string name) : base(new DateTimeNowTimeSource())
        {
            Component = new Component(this);
            _entities = Entity.CreateSelfManagingCollection(this);

            RegisterEventAppliers()
                .For<RootEvent.PropertyUpdated.Name>(e => Name = e.Name);

            RaiseEvent(new RootEvent.Implementation.Created(Guid.NewGuid(), name));
        }

        public IReadOnlyEntityCollection<Entity, Guid> Entities => _entities.Entities;
        public Entity AddEntity(string name) { return _entities.Add(new RootEvent.Entity.Implementation.Created(Guid.NewGuid(), name)); }
    }
}
