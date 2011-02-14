using Composable.DDD;
using NUnit.Framework;

namespace Composable.AutoMapper.Tests
{
    [TestFixture]
    public class WithoutConfiguration
    {
        public struct StructA
        {
            public string Title { get; set; }
        }

        public class ClassA : ValueObject<ClassA>
        {
            public string Val1 { get; set; }
            public string Val2;
        }


        [Test]
        public void MappingToSameTypeWorks()
        {
            var structA = new StructA {Title = "A"};
            var classA = new ClassA() {Val1 = "1", Val2 = "2"};

            Assert.That(structA.MapTo<StructA>(), Is.EqualTo(structA));
            Assert.That(classA.MapTo<ClassA>(), Is.EqualTo(classA));
        }
    }
}