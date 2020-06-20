 // ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global
#pragma warning disable IDE1006 //Review OK: Naming Styles
namespace Composable.Tests.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain.Events
{
    static partial class RootEvent
    {
        public static partial class Component
        {
            internal static class NestedComponent
            {
                internal interface IRoot : Component.IRoot { }
                internal interface Renamed : IRoot, PropertyUpdated.Name {}

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
                    public class Renamed : Root, NestedComponent.Renamed
                    {
                        public Renamed(string name) => Name = name;
                        public string Name { get; }
                    }
                }
            }
        }
    }
}
