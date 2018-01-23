using System;
using Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.GuidId.Domain.Events;

namespace Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.GuidId.QueryModels
{
    partial class Entity
    {
        public class NestedEntity : NestedEntity<NestedEntity,
                                        Guid,
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
        }
    }
}