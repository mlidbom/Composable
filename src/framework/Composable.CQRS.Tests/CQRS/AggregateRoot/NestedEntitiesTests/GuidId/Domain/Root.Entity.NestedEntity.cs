using System;
using Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.GuidId.Domain.Events;

namespace Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.GuidId.Domain
{
    partial class RemovableEntity
    {
        public class RemovableNestedEntity : RemovableNestedEntity<RemovableNestedEntity,
                                        Guid,
                                        RootEvent.Entity.NestedEntity.Implementation.Root,
                                        RootEvent.Entity.NestedEntity.IRoot,
                                        RootEvent.Entity.NestedEntity.Created,
                                        RootEvent.Entity.NestedEntity.Removed,
                                        RootEvent.Entity.NestedEntity.Implementation.Root.IdGetterSetter>
        {
            public string Name { get; private set; }
            public RemovableNestedEntity(RemovableEntity removableEntity) : base(removableEntity)
            {
                RegisterEventAppliers()
                    .For<RootEvent.Entity.NestedEntity.PropertyUpdated.Name>(e => Name = e.Name);
            }

            public void Rename(string name) => Publish(new RootEvent.Entity.NestedEntity.Implementation.Renamed(name: name));
            public void Remove() => Publish(new RootEvent.Entity.NestedEntity.Implementation.Removed());
        }
    }
}