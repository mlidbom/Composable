#region

using System.Linq;
using AutoMapper;
using Composable.DDD;
using NUnit.Framework;
using Composable.System.Linq;

#endregion

namespace Composable.AutoMapper.Tests
{
    [TestFixture]
    public class WhenMappingValueObjects
    {
        private IMappingEngine Engine { get; set; }

        [SetUp]
        public void Setup()
        {
            Engine = ComposableMappingEngine.BuildEngine(
                configuration =>
                    {
                        configuration.CreateMap<A, B>();
                        configuration.CreateMap(typeof (B), typeof (A));
                    });
        }

        [Test]
        public void ManuallyConfiguredMappingsWorkForObjects()
        {
            var a = new A("A");
            var b = new B("A");

            Assert.That(a.MapTo<B>(Engine), Is.EqualTo(b));
            Assert.That(Engine.Map(a).To<B>(), Is.EqualTo(b));
            Assert.That(b.MapTo<A>(Engine), Is.EqualTo(a));
            Assert.That(Engine.Map(b).To<A>(), Is.EqualTo(a));

            Assert.That(a.MapTo(typeof(B), Engine), Is.EqualTo(b));
            Assert.That(Engine.Map(a).To(typeof(B)), Is.EqualTo(b));
            Assert.That(b.MapTo(typeof(A), Engine), Is.EqualTo(a));
            Assert.That(Engine.Map(b).To(typeof(A)), Is.EqualTo(a));
        }

        [Test]
        public void ManuallyConfiguredMappingsWorkForCollections()
        {
            var a = 1.Through(10).Select( num => new A("A" + num )).ToList();
            var b = 1.Through(10).Select(num => new B("A" + num)).ToList();

            Assert.That(a.MapCollectionTo<B>(Engine), Is.EqualTo(b));
            Assert.That(Engine.MapCollection(a).To<B>(), Is.EqualTo(b));
            Assert.That(b.MapCollectionTo<A>(Engine), Is.EqualTo(a));
            Assert.That(Engine.MapCollection(b).To<A>(), Is.EqualTo(a));

            Assert.That(a.MapCollectionTo(typeof(B), Engine), Is.EqualTo(b));
            Assert.That(Engine.MapCollection(a).To(typeof(B)), Is.EqualTo(b));
            Assert.That(b.MapCollectionTo(typeof(A), Engine), Is.EqualTo(a));
            Assert.That(Engine.MapCollection(b).To(typeof(A)), Is.EqualTo(a));
        }


        [Test]
        public void RoundTrippingWorks()
        {
            var a = new A("A");
            Assert.That(a.MapTo<B>(Engine).MapTo<A>(Engine), Is.EqualTo(a));
            Assert.That(Engine.Map(Engine.Map(a).To<B>()).To<A>(), Is.EqualTo(a));
        }

        [Test]
        public void MapOntoResultsInIdenticalProperties()
        {
            var a = new A("A");
            var b = new B("B");

            a.MapOnto(b, Engine);
            Assert.That(b.MapTo<A>(Engine), Is.EqualTo(a));

            Engine.Map(a).OnTo(b);
            Assert.That(b.MapTo<A>(Engine), Is.EqualTo(a));
        }

        [Test]
        public void MapDynamicOntoResultsInIdenticalProperties()
        {
            var a = new A("A");
            var b = new B("B");

            a.MapDynamicOnto(b, Engine);
            Assert.That(b.MapTo<A>(Engine), Is.EqualTo(a));

            Engine.Map(a).DynamicOnto(b);
            Assert.That(b.MapTo<A>(Engine), Is.EqualTo(a));
        }

        public class A : ValueObject<A>
        {
            public string P1 { get; set; }
            public string P2 { get; private set; }

            //Members with hidden getter are not mapped!
            //private string P3 { get; set; }

            //protected string P4 { get; private set; }
            //protected string P5 { get; set; }

            public string P6 { get; set; }

            public string P7 { get; private set; }

            //private string _p8;
            //private string P8 { get { return _p8; } set { _p8 = value; } }

            protected A() : this("A")
            {
            }

            public A(string value)
            {
                P1 = P2 = P6 = P7 = value;
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

            public string P6 { get; set; }

            public string P7 { get; private set; }

            //private string _p8;
            //private string P8 { get { return _p8; } set { _p8 = value; } }

            protected B() : this("B")
            {
            }

            public B(string value)
            {
                P1 = P2 = P6 = P7 = value;
            }
        }
    }
}