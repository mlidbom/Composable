using System;
using Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels;
using Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.GuidId.Domain.Events;

namespace Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.GuidId.QueryModels
{
    partial class Entity : RootQueryModel.Entity<Entity,
                              Guid,
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

        public IReadonlyQueryModelEntityCollection<NestedEntity, Guid> Entities => _entities.Entities;
        readonly NestedEntity.CollectionManager _entities;
    }
}