using System;
using Composable.CQRS.CQRS.EventSourcing;
using Composable.CQRS.Tests.CQRS.EventSourcing.AggregateRoot.NestedEntitiesTests.GuidId.Domain.Events;
using JetBrains.Annotations;

namespace Composable.CQRS.Tests.CQRS.EventSourcing.AggregateRoot.NestedEntitiesTests.GuidId.Domain
{
    [UsedImplicitly]
    public partial class Entity : Root.Entity<Entity,
                              Guid,
                              RootEvent.Entity.Implementation.Root,
                              RootEvent.Entity.IRoot,
                              RootEvent.Entity.Created,
                              RootEvent.Entity.Removed,
                              RootEvent.Entity.Implementation.Root.IdGetterSetter>
    {
        public string Name { get; private set; }
        public Root Root { get; }
        public Entity(Root root) : base(root)
        {
            Root = root;
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