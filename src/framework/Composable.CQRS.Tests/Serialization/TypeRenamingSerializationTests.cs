using System.Collections.Generic;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using FluentAssertions;
using NUnit.Framework;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Composable.Tests.Serialization
{
    namespace OriginalTypes
    {
        class BaseTypeA {}

        class TypeA : BaseTypeA
        {
            internal static TypeA Create() => new TypeA {TypeAName = typeof(TypeA).FullName};

            public string TypeAName { get; set; }

            public class TypeAA : TypeA
            {
                public new static TypeA Create() => new TypeAA() {TypeAAName = typeof(TypeAA).FullName};
                public string TypeAAName { get; set; }
            }
        }

        class TypeB : BaseTypeA
        {
            internal static TypeB Create() => new TypeB {TypeBName = typeof(TypeB).FullName};
            public string TypeBName { get; set; }

            public class TypeBB : TypeB
            {
                public new static TypeBB Create() => new TypeBB() {TypeBBName = typeof(TypeBB).FullName};
                public string TypeBBName { get; set; }
            }
        }

        class Root
        {
            internal static Root Create() => new Root()
                                             {
                                                 TypeA = OriginalTypes.TypeA.Create(),
                                                 TypeB = OriginalTypes.TypeB.Create(),
                                                 ListOfTypeA = new List<BaseTypeA>() {OriginalTypes.TypeA.Create(), OriginalTypes.TypeB.Create(), OriginalTypes.TypeA.TypeAA.Create(), OriginalTypes.TypeB.TypeBB.Create()}
                                             };

            public BaseTypeA TypeA { get; set; }
            public BaseTypeA TypeB { get; set; }

            public List<BaseTypeA> ListOfTypeA { get; set; }
        }
    }

    namespace RenamedTypes
    {
        class BaseTypeA {}

        class TypeA : BaseTypeA
        {
            public string TypeAName { get; set; }

            public class TypeAA : TypeA
            {
                public string TypeAAName { get; set; }
            }
        }

        class TypeB : BaseTypeA
        {
            public string TypeBName { get; set; }

            public class TypeBB : TypeB
            {
                public string TypeBBName { get; set; }
            }
        }

        class Root
        {
            public BaseTypeA TypeA { get; set; }
            public BaseTypeA TypeB { get; set; }

            public List<BaseTypeA> ListOfTypeA { get; set; }
        }
    }

    [TestFixture] public class TypeRenamingSerializationTests
    {
        ITypeMapper _originaltypesMap;
        ITypeMapper _renamedTypesMap;
        RenamingSupportingJsonSerializer _originalTypesSerializer;
        RenamingSupportingJsonSerializer _renamedTypesSerializer;
        ITestingEndpointHost _originalHost;
        ITestingEndpointHost _renamedHost;

        static class Ids
        {
            internal const string TypeA = "5A4DACAF-FAE1-4D8A-87AA-99E84CE4819B";
            internal const string TypeAA = "d774e63b-c796-4219-8570-882cceb072a3";
            internal const string TypeB = "AADA2B9D-62BC-4C81-ADF1-E8075F41D2BA";
            internal const string TypeBB = "243C4874-529F-44B6-91BE-1353DB87AAEE";
        }

        [OneTimeSetUp] public void SetupTask()
        {
            _originalHost = EndpointHost.Testing.CreateHost(DependencyInjectionContainer.Create);
            _renamedHost = EndpointHost.Testing.CreateHost(DependencyInjectionContainer.Create);

            _originaltypesMap = _originalHost.RegisterTestingEndpoint(
                setup: builder => builder.TypeMapper
                                         .Map<OriginalTypes.TypeA>(Ids.TypeA)
                                         .Map<OriginalTypes.TypeB>(Ids.TypeB)
                                         .Map<OriginalTypes.TypeA.TypeAA>(Ids.TypeAA)
                                         .Map<OriginalTypes.TypeB.TypeBB>(Ids.TypeBB)).ServiceLocator.Resolve<ITypeMapper>();

            _renamedTypesMap = _originalHost.RegisterTestingEndpoint(
                setup: builder => builder.TypeMapper
                                         .Map<RenamedTypes.TypeA>(Ids.TypeA)
                                         .Map<RenamedTypes.TypeB>(Ids.TypeB)
                                         .Map<RenamedTypes.TypeA.TypeAA>(Ids.TypeAA)
                                         .Map<RenamedTypes.TypeB.TypeBB>(Ids.TypeBB)).ServiceLocator.Resolve<ITypeMapper>();

            _originalTypesSerializer = new RenamingSupportingJsonSerializer(JsonSettings.JsonSerializerSettings, _originaltypesMap);
            _renamedTypesSerializer = new RenamingSupportingJsonSerializer(JsonSettings.JsonSerializerSettings, _renamedTypesMap);
        }

        [OneTimeTearDown] public void TearDownTask()
        {
            _originalHost.Dispose();
            _renamedHost.Dispose();
        }

        [Test] public void Roundtrips_polymorphic_types_types()
        {
            var originalRoot = OriginalTypes.Root.Create();
            var originalJson = _originalTypesSerializer.Serialize(originalRoot);
            var deserializedRoot = (OriginalTypes.Root)_originalTypesSerializer.Deserialize(typeof(OriginalTypes.Root), originalJson);

            deserializedRoot.ShouldBeEquivalentTo(originalRoot, options => options.RespectingRuntimeTypes());
            originalRoot.ShouldBeEquivalentTo(deserializedRoot, options => options.RespectingRuntimeTypes());
        }

        [Test] public void Handles_renaming_of_types()
        {
            var originalRoot = OriginalTypes.Root.Create();
            var originalJson = _originalTypesSerializer.Serialize(originalRoot);

            var deserializedRenamedRoot = (RenamedTypes.Root)_renamedTypesSerializer.Deserialize(typeof(RenamedTypes.Root), originalJson);

            deserializedRenamedRoot.ShouldBeEquivalentTo(originalRoot, options => options.RespectingRuntimeTypes());
            originalRoot.ShouldBeEquivalentTo(deserializedRenamedRoot, options => options.RespectingRuntimeTypes());
        }
    }
}
