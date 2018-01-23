using Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.GuidId.Domain.Events;

namespace Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.GuidId.QueryModels
{
    partial class Component
    {
        public class NestedComponent : Component.NestedComponent<NestedComponent, RootEvent.Component.NestedComponent.IRoot>
        {
            public NestedComponent(Component parent) : base(parent)
            {
                RegisterEventAppliers()
                    .For<RootEvent.Component.NestedComponent.PropertyUpdated.Name>(e => {});
            }
        }
    }
}