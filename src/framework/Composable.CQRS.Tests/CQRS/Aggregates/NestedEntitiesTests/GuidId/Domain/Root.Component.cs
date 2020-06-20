using System;
using Composable.Persistence.EventStore.Aggregates;
using Composable.Tests.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain.Events;

namespace Composable.Tests.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain
{
    partial class Component : Root.Component<Component, RootEvent.Component.Implementation.Root, RootEvent.Component.IRoot>
    {
        public Component(Root root) : base(root)
        {
            _entities = Component.Entity.CreateSelfManagingCollection(this);
            CComponent = new NestedComponent(this);
            RegisterEventAppliers()
                .For<RootEvent.Component.PropertyUpdated.Name>(e => Name = e.Name);
        }

        readonly Component.Entity.CollectionManager _entities;

        public NestedComponent CComponent { get; private set; }

        public string Name { get; private set; } = string.Empty;
        public IReadOnlyEntityCollection<Entity, Guid> Entities => _entities.Entities;
        public void Rename(string name) { Publish(new RootEvent.Component.Implementation.Renamed(name)); }
        public Component.Entity AddEntity(string name, Guid id) => _entities.AddByPublishing(new RootEvent.Component.Entity.Implementation.Created(id, name));
    }
}