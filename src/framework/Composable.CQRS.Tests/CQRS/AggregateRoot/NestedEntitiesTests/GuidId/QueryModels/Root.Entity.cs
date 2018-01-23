using System;
using Composable.Persistence.EventStore.AggregateRoots;
using Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.GuidId.Domain.Events;

namespace Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.GuidId.QueryModels
{
    partial class Entity : RootQueryModel.Entity<Entity,
                              Guid,
                              RootEvent.Entity.Implementation.Root,
                              RootEvent.Entity.IRoot,
                              RootEvent.Entity.Created,
                              RootEvent.Entity.Removed,
                              RootEvent.Entity.Implementation.Root.IdGetterSetter>
    {
        public string Name { get; private set; }
        public Entity(RootQueryModel root) : base(root)
        {
            _entities = NestedEntity.CreateSelfManagingCollection(this);
            RegisterEventAppliers()
                .For<RootEvent.Entity.PropertyUpdated.Name>(e => Name = e.Name);
        }

        public IReadOnlyEntityCollection<NestedEntity, Guid> Entities => _entities.Entities;
        readonly NestedEntity.CollectionManager _entities;

        public void Rename(string name) { Publish(new RootEvent.Entity.Implementation.Renamed(name)); }
        public void Remove() => Publish(new RootEvent.Entity.Implementation.Removed());

        public NestedEntity AddEntity(string name)
            => _entities.AddByPublishing(new RootEvent.Entity.NestedEntity.Implementation.Created(nestedEntityId: Guid.NewGuid(), name: name));
    }
}