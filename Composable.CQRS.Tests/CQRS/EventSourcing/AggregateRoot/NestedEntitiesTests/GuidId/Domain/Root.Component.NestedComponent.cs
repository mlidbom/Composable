using Composable.CQRS.Tests.CQRS.EventSourcing.AggregateRoot.NestedEntitiesTests.GuidId.Domain.Events;

namespace Composable.CQRS.Tests.CQRS.EventSourcing.AggregateRoot.NestedEntitiesTests.GuidId.Domain
{
    partial class Component
    {
        public class NestedComponent : Component.NestedComponent<NestedComponent, RootEvent.Component.NestedComponent.Implementation.Root, RootEvent.Component.NestedComponent.IRoot>
        {
            string Name { get; set; }
            public NestedComponent(Component parent) : base(parent)
            {
                RegisterEventAppliers()
                    .For<RootEvent.Component.NestedComponent.PropertyUpdated.Name>(e => Name = e.Name);
            }
        }
    }
}