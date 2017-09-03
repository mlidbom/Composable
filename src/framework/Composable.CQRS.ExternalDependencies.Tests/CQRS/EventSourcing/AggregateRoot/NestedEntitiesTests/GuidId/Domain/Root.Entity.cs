using System;
using Composable.CQRS.Tests.CQRS.EventSourcing.AggregateRoot.NestedEntitiesTests.GuidId.Domain.Events;
using Composable.Persistence.EventStore.AggregateRoots;

namespace Composable.CQRS.Tests.CQRS.EventSourcing.AggregateRoot.NestedEntitiesTests.GuidId.Domain
{
    partial class Entity : Root.Entity<Entity,
                              Guid,
                              RootEvent.Entity.Implementation.Root,
                              RootEvent.Entity.IRoot,
                              RootEvent.Entity.Created,
                              RootEvent.Entity.Removed,
                              RootEvent.Entity.Implementation.Root.IdGetterSetter>
    {
        public string Name { get; private set; }
        public Entity(Root root) : base(root)
        {
            _entities = NestedEntity.CreateSelfManagingCollection(this);
            RegisterEventAppliers()
                .For<RootEvent.Entity.PropertyUpdated.Name>(e => Name = e.Name);
        }

        public IReadOnlyEntityCollection<NestedEntity, Guid> Entities => _entities.Entities;
        readonly NestedEntity.CollectionManager _entities;

        public void Rename(string name) { RaiseEvent(new RootEvent.Entity.Implementation.Renamed(name)); }
        public void Remove() => RaiseEvent(new RootEvent.Entity.Implementation.Removed());

        public NestedEntity AddEntity(string name)
            => _entities.Add(new RootEvent.Entity.NestedEntity.Implementation.Created(nestedEntityId: Guid.NewGuid(), name: name));
    }
}