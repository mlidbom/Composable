using System;
using NUnit.Framework;

namespace Composable.AutoMapper.Tests
{
    [TestFixture]
    public class WhenMoreThanOneDefinitionOfAMappingExists
    {
        public class MappingBootstrapper : IProvidesMappings
        {
            public void CreateMappings(SafeConfiguration configuration)
            {
                configuration.CreateMap<A, B>();
                configuration.CreateMap<A, B>();
            }
        }

        [Test]
        public void DuplicateMappingExceptionIsThrown()
        {
            Assert.Throws<DuplicateMappingException>(() => new A().MapTo<B>());
        }

        public class A
        {
        }

        public class B
        {
        }
    }
}