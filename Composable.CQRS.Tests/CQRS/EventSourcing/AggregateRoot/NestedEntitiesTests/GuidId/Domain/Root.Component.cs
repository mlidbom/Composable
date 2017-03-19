using System;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.Tests.CQRS.EventSourcing.AggregateRoot.NestedEntitiesTests.GuidId.Domain.Events;

namespace Composable.CQRS.Tests.CQRS.EventSourcing.AggregateRoot.NestedEntitiesTests.GuidId.Domain
{
    partial class Component : Root.Component<Component, RootEvent.Component.Implementation.Root, RootEvent.Component.IRoot>
    {
        public Component(Root root) : base(root)
        {
            _entities = Component.Entity.CreateSelfManagingCollection(this);
            _nestedComponent = new NestedComponent(this);
            RegisterEventAppliers()
                .For<RootEvent.Component.PropertyUpdated.Name>(e => Name = e.Name);
        }

        readonly Component.Entity.CollectionManager _entities;
        // ReSharper disable once NotAccessedField.Local
        NestedComponent _nestedComponent;

        public string Name { get; private set; }
        public IReadOnlyEntityCollection<Entity, Guid> Entities => _entities.Entities;
        public void Rename(string name) { RaiseEvent(new RootEvent.Component.Implementation.Renamed(name)); }
        public Component.Entity AddEntity(string name) => _entities.Add(new RootEvent.Component.Entity.Implementation.Created(Guid.NewGuid(), name));
    }
}