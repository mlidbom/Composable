using Composable.Tests.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain.Events;

namespace Composable.Tests.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain
{
    partial class Component
    {
        public class NestedComponent : Component.NestedComponent<NestedComponent, RootEvent.Component.NestedComponent.Implementation.Root, RootEvent.Component.NestedComponent.IRoot>
        {
            public NestedComponent(Component parent) : base(parent)
            {
                RegisterEventAppliers()
                   .For<RootEvent.Component.NestedComponent.PropertyUpdated.Name>(e => Name = e.Name);
            }

            public string Name { get; private set; }

            public void Rename(string name) => Publish(new RootEvent.Component.NestedComponent.Implementation.Renamed(name));
        }
    }
}