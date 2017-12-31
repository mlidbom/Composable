 // ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.GuidId.Domain.Events
{
    static partial class RootEvent
    {
        public static partial class Component
        {
            internal static class NestedComponent
            {
                [TypeId("483016BA-1C16-4C92-A0ED-3F4A5C3BB3AA")]internal interface IRoot : Component.IRoot { }

                internal static class PropertyUpdated
                {
                    [TypeId("C770318F-889E-406E-B58B-366A0FD63099")]public interface Name : NestedComponent.IRoot
                    {
                    }
                }

                internal static class Implementation
                {
                    [TypeId("975A7BF8-AA54-4083-95AF-A42A1305A232")]public abstract class Root : Component.Implementation.Root, NestedComponent.IRoot { }
                }
            }
        }
    }
}
