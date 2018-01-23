using System;
using Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels;
using Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.GuidId.Domain.Events;

namespace Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.GuidId.QueryModels
{
    partial class Component : RootQueryModel.Component<Component, RootEvent.Component.IRoot>
    {
        public Component(RootQueryModel root) : base(root)
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
        public IReadonlyQueryModelEntityCollection<Entity, Guid> Entities => _entities.Entities;
    }
}