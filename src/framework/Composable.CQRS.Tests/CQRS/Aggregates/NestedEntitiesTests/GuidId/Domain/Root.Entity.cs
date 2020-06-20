using System;
using Composable.Persistence.EventStore.Aggregates;
using Composable.Tests.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain.Events;

namespace Composable.Tests.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain
{
    partial class RemovableEntity : Root.RemovableEntity<RemovableEntity,
                                                         Guid,
                                                         RootEvent.Entity.Implementation.Root,
                                                         RootEvent.Entity.IRoot,
                                                         RootEvent.Entity.Created,
                                                         RootEvent.Entity.Removed,
                                                         RootEvent.Entity.Implementation.Root.IdGetterSetter>
    {
        public string Name { get; private set; } = string.Empty;
        public RemovableEntity(Root root) : base(root)
        {
            _entities = RemovableNestedEntity.CreateSelfManagingCollection(this);
            RegisterEventAppliers()
                .For<RootEvent.Entity.PropertyUpdated.Name>(e => Name = e.Name);
        }

        public IReadOnlyEntityCollection<RemovableNestedEntity, Guid> Entities => _entities.Entities;
        readonly RemovableNestedEntity.CollectionManager _entities;

        public void Rename(string name) { Publish(new RootEvent.Entity.Implementation.Renamed(name)); }
        public void Remove() => Publish(new RootEvent.Entity.Implementation.Removed());

        public RemovableNestedEntity AddEntity(string name, Guid id)
            => _entities.AddByPublishing(new RootEvent.Entity.NestedEntity.Implementation.Created(id: id, name: name));
    }
}