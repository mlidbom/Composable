 // ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Composable.CQRS.Tests.CQRS.EventSourcing.AggregateRoot.NestedEntitiesTests.GuidId.Domain.Events
{
    static partial class RootEvent
    {
        public static partial class Component
        {
            internal static partial class NestedComponent
            {
                internal interface IRoot : Component.IRoot { }

                internal static class PropertyUpdated
                {
                    public interface Name : NestedComponent.IRoot
                    {
                        string Name { get; }
                    }
                }

                internal static class Implementation
                {
                    public abstract class Root : Component.Implementation.Root, NestedComponent.IRoot { }
                }
            }
        }
    }
}
