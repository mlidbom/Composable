using System.Collections.Generic;
using Composable.DependencyInjection;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using NUnit.Framework;

namespace Composable.Tests.Serialization
{
    namespace OriginalTypes
    {
        class BaseTypeA
        {
        }

        class TypeA : BaseTypeA
        {
        }

        class TypeB : BaseTypeA
        {

        }

        class Root
        {
            public BaseTypeA TypeA { get; set; } = new TypeA();
            public BaseTypeA TypeB { get; set; } = new TypeB();

            public List<BaseTypeA> ListOfTypeA { get; set; } = new List<BaseTypeA>(){ new TypeA(), new TypeA(), new TypeA()};
        }
    }

    namespace RenamedTypes
    {
        class BaseTypeA
        {
        }

        class TypeA : BaseTypeA
        {
        }

        class TypeB : BaseTypeA
        {

        }

        class Root
        {
            public BaseTypeA TypeA { get; set; } = new TypeA();
            public BaseTypeA TypeB { get; set; } = new TypeB();

            public List<BaseTypeA> ListOfTypeA { get; set; } = new List<BaseTypeA>(){ new TypeA(), new TypeA(), new TypeA()};
        }
    }

    [TestFixture]
    public class TypeRenamingSerializationTests
    {
        [Test] public void Handles_renaming_of_types()
        {
            var originaltypes = new TypeMapper();
            originaltypes.Map<OriginalTypes.TypeA>("5A4DACAF-FAE1-4D8A-87AA-99E84CE4819B")
                         .Map<OriginalTypes.TypeB>("AADA2B9D-62BC-4C81-ADF1-E8075F41D2BA");

            var originalTypesSerializer = new RenamingSupportingJsonSerializer(JsonSettings.JsonSerializerSettings, originaltypes);

            var renamedTypes = new TypeMapper();
            renamedTypes.Map<OriginalTypes.TypeA>("5A4DACAF-FAE1-4D8A-87AA-99E84CE4819B")
                        .Map<OriginalTypes.TypeB>("AADA2B9D-62BC-4C81-ADF1-E8075F41D2BA");

            var renamedTypesSerializer = new RenamingSupportingJsonSerializer(JsonSettings.JsonSerializerSettings, renamedTypes);


        }

        void ConfigureWithNewTypeMappings(IDependencyInjectionContainer container)
        {
            
        }

        void ConfigureWithOriginalTypeMappings(IDependencyInjectionContainer container)
        {

        }
    }
}