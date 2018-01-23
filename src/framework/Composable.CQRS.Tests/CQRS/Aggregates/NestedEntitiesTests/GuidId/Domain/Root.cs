using System;
using Composable.GenericAbstractions.Time;
using Composable.Persistence.EventStore.Aggregates;
using Composable.Tests.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain.Events;

namespace Composable.Tests.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain
{
    class Root : Aggregate<Root, RootEvent.Implementation.Root, RootEvent.IRoot>
    {
        public string Name { get; private set; }
        readonly RemovableEntity.CollectionManager _entities;
#pragma warning disable 108,114
        public Component Component { get; private set; }
#pragma warning restore 108,114

        public Root(string name) : base(new DateTimeNowTimeSource())
        {
            Component = new Component(this);
            _entities = RemovableEntity.CreateSelfManagingCollection(this);

            RegisterEventAppliers()
                .For<RootEvent.PropertyUpdated.Name>(e => Name = e.Name);

            Publish(new RootEvent.Implementation.Created(Guid.NewGuid(), name));
        }

        public IReadOnlyEntityCollection<RemovableEntity, Guid> Entities => _entities.Entities;
        public RemovableEntity AddEntity(string name) => _entities.AddByPublishing(new RootEvent.Entity.Implementation.Created(Guid.NewGuid(), name));
    }
}
