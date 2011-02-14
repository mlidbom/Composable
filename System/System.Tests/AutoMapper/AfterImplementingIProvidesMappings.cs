using System;
using AutoMapper;
using NUnit.Framework;

namespace Composable.AutoMapper.Tests
{
    [TestFixture]
    public class AfterImplementingIProvidesMappings : MappingTest
    {
        A a = new A() { Title = "A" };
        B b = new B() { Title = "A" };

        [Test]
        public void ManuallyConfiguredMappingsWork()
        {
            Assert.That(a.MapTo<B>(), Is.EqualTo(b));
            Assert.That(b.MapTo<A>(), Is.EqualTo(a));
            
            Assert.That(a.MapTo(typeof(B)), Is.EqualTo(b));
            Assert.That(b.MapTo(typeof(A)), Is.EqualTo(a));
        }


        [Test]
        public void RoundTrippingWorks()
        {
            Assert.That(a.MapTo<B>().MapTo<A>(), Is.EqualTo(a));
        }


        public class MappingBootStrapper : IProvidesMappings
        {
            public void CreateMappings(SafeConfiguration c)
            {
                c.CreateMap<A, B>();
                c.CreateMap<B, A>();
            }
        }


        public struct A
        {
            public string Title { get; set; }
        }

        public struct B
        {
            public string Title { get; set; }
        }
    }
}