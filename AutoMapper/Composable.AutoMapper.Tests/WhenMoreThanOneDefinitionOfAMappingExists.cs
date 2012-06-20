#region usings

using AutoMapper;
using NUnit.Framework;

#endregion

namespace Composable.AutoMapper.Tests
{
    [TestFixture]
    public class WhenMoreThanOneDefinitionOfAMappingExists
    {
        [Test]
        public void DuplicateMappingExceptionIsThrown()
        {
            Assert.Throws<DuplicateMappingException>(() => ComposableMappingEngine.BuildEngine(configuration =>
                                                      {
                                                          configuration.CreateMap<A, B>();
                                                          configuration.CreateMap<A, B>();
                                                      }));
        }

        public class A
        {
        }

        public class B
        {
        }
    }
}