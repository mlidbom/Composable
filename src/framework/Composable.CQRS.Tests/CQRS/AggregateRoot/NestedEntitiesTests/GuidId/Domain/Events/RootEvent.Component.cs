 // ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.GuidId.Domain.Events
{
    static partial class RootEvent
    {
        public static partial class Component
        {
            [TypeId("8797D9C9-4615-40D5-AB44-A8545D69D73F")]public interface IRoot : RootEvent.IRoot {}

            [TypeId("303370CF-E905-49F2-A612-834D7A89C730")]interface Renamed : IRoot, PropertyUpdated.Name {}

            public static class PropertyUpdated
            {
                [TypeId("9AFD7261-136C-405B-810E-982755E96BE4")]public interface Name : IRoot
                {
                    string Name { get; }
                }
            }

            internal static class Implementation
            {
                [TypeId("14E025EA-31EE-482F-9BDE-DA8CF9DF07A6")]public abstract class Root : RootEvent.Implementation.Root, Component.IRoot {}

                [TypeId("61E15C83-A760-4881-8753-9140C8FB11CA")]public class Renamed : Root, Component.Renamed
                {
                    public Renamed(string name) => Name = name;
                    public string Name { get; }
                }
            }
        }
    }
}
