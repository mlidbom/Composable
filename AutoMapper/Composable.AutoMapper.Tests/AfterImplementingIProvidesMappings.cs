using System;
using AutoMapper;
using Composable.DDD;
using NUnit.Framework;

namespace Composable.AutoMapper.Tests
{
    [TestFixture]
    public class AfterImplementingIProvidesMappings : MappingTest
    {
        [Test]
        public void ManuallyConfiguredMappingsWork()
        {
            var a = new A("A");
            var b = new B("A");

            Assert.That(a.MapTo<B>(), Is.EqualTo(b));
            Assert.That(b.MapTo<A>(), Is.EqualTo(a));
            
            Assert.That(a.MapTo(typeof(B)), Is.EqualTo(b));
            Assert.That(b.MapTo(typeof(A)), Is.EqualTo(a));
        }


        [Test]
        public void RoundTrippingWorks()
        {
            var a = new A("A");
            Assert.That(a.MapTo<B>().MapTo<A>(), Is.EqualTo(a));
        }

        [Test]
        public void MapOntoResultsInIdenticalProperties()
        {
            var a = new A("A");
            var b = new B("B");

            a.MapOnto(b);
            Assert.That(b.MapTo<A>(), Is.EqualTo(a));
        }

        [Test]
        public void MapDynamicOntoResultsInIdenticalProperties()
        {
            var a = new A("A");
            var b = new B("B");

            a.MapDynamicOnto(b);
            Assert.That(b.MapTo<A>(), Is.EqualTo(a));
        }


        public class MappingBootStrapper : IProvidesMappings
        {
            public void CreateMappings(SafeConfiguration c)
            {
                c.CreateMap<A, B>();
                c.CreateMap<B, A>();
            }
        }


        public class A : ValueObject<A>
        {
            public string P1 { get; set; }
            public string P2 { get; private set; }

            //Members with hidden getter are not mapped!
            //private string P3 { get; set; }

            //protected string P4 { get; private set; }
            //protected string P5 { get; set; }

            private string _p6;
            public string P6 { get { return _p6; } set { _p6 = value; } }

            private string _p7;
            public string P7 { get { return _p6; } private set { _p6 = value; } }

            private string _p8;
            private string P8 { get { return _p6; } set { _p6 = value; } }

            protected A():this("A"){}
            public A(string value)
            {
                P1 = P2 = P6 = P7 = P8 = value;
            }

        }

        public class B : ValueObject<B>
        {
            public string P1 { get; set; }
            public string P2 { get; private set; }

            //Members with hidden getter are not mapped!
            //private string P3 { get; set; }

            //protected string P4 { get; private set; }
            //protected string P5 { get; set; }

            private string _p6;
            public string P6 { get { return _p6; } set { _p6 = value; } }

            private string _p7;
            public string P7 { get { return _p6; } private set { _p6 = value; } }

            private string _p8;
            private string P8 { get { return _p6; } set { _p6 = value; } }

            protected B():this("B"){}
            public B(string value)
            {
                P1 = P2 = P6 = P7 = P8 = value;
            }
        }
    }
}