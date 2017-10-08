using System;
using Composable.CQRS.Tests.CQRS.EventSourcing.AggregateRoot.NestedEntitiesTests.GuidId.Domain.Events;

namespace Composable.CQRS.Tests.CQRS.EventSourcing.AggregateRoot.NestedEntitiesTests.GuidId.Domain
{
    partial class Entity
    {
        public class NestedEntity : NestedEntity<NestedEntity,
                                        Guid,
                                        RootEvent.Entity.NestedEntity.Implementation.Root,
                                        RootEvent.Entity.NestedEntity.IRoot,
                                        RootEvent.Entity.NestedEntity.Created,
                                        RootEvent.Entity.NestedEntity.Removed,
                                        RootEvent.Entity.NestedEntity.Implementation.Root.IdGetterSetter>
        {
            public string Name { get; private set; }
            public NestedEntity(Entity entity) : base(entity)
            {
                RegisterEventAppliers()
                    .For<RootEvent.Entity.NestedEntity.PropertyUpdated.Name>(e => Name = e.Name);
            }

            public void Rename(string name) => RaiseEvent(new RootEvent.Entity.NestedEntity.Implementation.Renamed(name: name));
            public void Remove() => RaiseEvent(new RootEvent.Entity.NestedEntity.Implementation.Removed());
        }
    }
}