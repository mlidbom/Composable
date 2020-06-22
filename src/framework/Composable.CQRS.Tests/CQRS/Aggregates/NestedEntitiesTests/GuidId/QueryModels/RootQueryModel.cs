using System;
using Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels;
using Composable.Tests.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain.Events;

namespace Composable.Tests.CQRS.Aggregates.NestedEntitiesTests.GuidId.QueryModels
{
    class RootQueryModel : SelfGeneratingQueryModel<RootQueryModel, RootEvent.IRoot>
    {
        public string Name { get; private set; } = string.Empty;
        readonly Entity.CollectionManager _entities;
        public Component Component { get; private set; }

        public RootQueryModel()
        {
            Component = new Component(this);
            _entities = Entity.CreateSelfManagingCollection(this);

            RegisterEventAppliers()
                .For<RootEvent.PropertyUpdated.Name>(e => Name = e.Name);
        }

        public IReadonlyQueryModelEntityCollection<Entity, Guid> Entities => _entities.Entities;
    }
}
