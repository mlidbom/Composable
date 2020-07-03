using System;
using Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels;
using Composable.Tests.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain.Events;

namespace Composable.Tests.CQRS.Aggregates.NestedEntitiesTests.GuidId.QueryModels
{
    partial class Entity : RootQueryModel.Entity<Entity,
                              Guid,
                              RootEvent.Entity.IRoot,
                              RootEvent.Entity.Created,
                              RootEvent.Entity.Removed,
                              RootEvent.Entity.Implementation.Root.IdGetterSetter>
    {
        public string Name { get; private set; } = string.Empty;
        public Entity(RootQueryModel root) : base(root)
        {
            _entities = RemovableNestedEntity.CreateSelfManagingCollection(this);
            RegisterEventAppliers()
                .For<RootEvent.Entity.PropertyUpdated.Name>(e => Name = e.Name);
        }

        public IReadonlyQueryModelEntityCollection<RemovableNestedEntity, Guid> Entities => _entities.Entities;
        readonly RemovableNestedEntity.CollectionManager _entities;
    }
}