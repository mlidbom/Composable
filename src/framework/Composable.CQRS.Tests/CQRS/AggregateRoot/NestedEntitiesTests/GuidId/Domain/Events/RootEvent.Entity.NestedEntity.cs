using System;
using Composable.Persistence.EventStore.AggregateRoots;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.GuidId.Domain.Events
{
    static partial class RootEvent
    {
        public static partial class Entity
        {
            public static class NestedEntity
            {
                [TypeId("545758B2-DDCC-4EE8-8324-36D355100E0F")]public interface IRoot : RootEvent.Entity.IRoot
                {
                    Guid NestedEntityId { get; }
                }

                [TypeId("80FE0081-C349-4626-8943-41022A45D3DF")]public interface Created : IRoot, PropertyUpdated.Name {}

                [TypeId("B752A942-D906-414E-ACEA-48EA0B7B6997")]interface Renamed : IRoot, PropertyUpdated.Name {}
                [TypeId("692DE3B4-DB36-4FF0-8C76-D9C9CE2F7DA1")]public interface Removed : IRoot { }

                public static class PropertyUpdated
                {
                    [TypeId("2073E6B6-BFFD-46C0-B43E-02B888EFC1CD")]public interface Name : IRoot
                    {
                        string Name { get; }
                    }
                }

                internal static class Implementation
                {
                    public abstract class Root : RootEvent.Entity.Implementation.Root, NestedEntity.IRoot
                    {
                        public Guid NestedEntityId { get; protected set; }

                        public new class IdGetterSetter : Root, IGetSetAggregateRootEntityEventEntityId<Guid, Root, IRoot>
                        {
                            public void SetEntityId(Root @event, Guid id) => @event.NestedEntityId = id;
                            public Guid GetId(IRoot @event) => @event.NestedEntityId;
                        }
                    }

                    [TypeId("D7CDB34E-3584-4F28-BB46-640C48FB313C")]public class Created : Root, NestedEntity.Created
                    {
                        public Created(Guid nestedEntityId, string name)
                        {
                            NestedEntityId = nestedEntityId;
                            Name = name;
                        }
                        public string Name { get; }
                    }

                    [TypeId("0599E9D2-BD94-420E-A13C-F17D9B4D14AB")]public class Renamed : Root, NestedEntity.Renamed
                    {
                        public Renamed(string name) => Name = name;
                        public string Name { get; }
                    }

                    [TypeId("78FABE1C-EB3D-4DDA-BE85-9D7BD6CFA6C9")]public class Removed : Root, NestedEntity.Removed
                    {
                    }
                }
            }
        }
    }
}
