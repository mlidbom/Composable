using System;
using Composable.CQRS.Tests.CQRS.EventSourcing.AggregateRoot.NestedEntitiesTests.GuidId.Domain.Events;

namespace Composable.CQRS.Tests.CQRS.EventSourcing.AggregateRoot.NestedEntitiesTests.GuidId.Domain
{
    partial class Component
    {
        public class Entity : Component.NestedEntity<Entity,
                                  Guid,
                                  RootEvent.Component.Entity.Implementation.Root,
                                  RootEvent.Component.Entity.IRoot,
                                  RootEvent.Component.Entity.Created,
                                  RootEvent.Component.Entity.Removed,
                                  RootEvent.Component.Entity.Implementation.Root.IdGetterSetter>
        {
            public string Name { get; private set; }
            public Entity(Component parent) : base(parent)
            {
                RegisterEventAppliers()
                    .For<RootEvent.Component.Entity.PropertyUpdated.Name>(e => Name = e.Name);
            }

            public void Rename(string name) { RaiseEvent(new RootEvent.Component.Entity.Implementation.Renamed(name)); }
            public void Remove() => RaiseEvent(new RootEvent.Component.Entity.Implementation.Removed());
        }
    }
}