#region

using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Composable.System.Linq;
using NUnit.Framework;

#endregion

namespace Composable.AutoMapper.Tests
{
    [TestFixture]
    public class WhenMappingStructs
    {
        private struct A
        {
            public string Title { get; set; }
        }

        private struct B
        {
            public string Title { get; set; }
        }

        private A _a = new A {Title = "A"};
        private B _b = new B {Title = "A"};

        private List<A> _aList = 1.Through(10).Select(num => new A {Title = "A" + num}).ToList();
        private List<B> _bList = 1.Through(10).Select(num => new B {Title = "A" + num}).ToList();

        private IMappingEngine _engine;

        [TestFixtureSetUp]
        public void Setup()
        {
            _engine = ComposableMappingEngine.BuildEngine(
                configuration =>
                    {
                        configuration.CreateMap<A, B>();
                        configuration.CreateMap<B, A>();
                    });
        }

        [Test]
        public void CanMapInstances()
        {
            Assert.That(_a.MapTo<B>(_engine), Is.EqualTo(_b));
            Assert.That(_engine.Map(_a).To<B>(), Is.EqualTo(_b));

            Assert.That(_b.MapTo<A>(_engine), Is.EqualTo(_a));
            Assert.That(_engine.Map(_b).To<A>(), Is.EqualTo(_a));

            Assert.That(_a.MapTo(typeof (B), _engine), Is.EqualTo(_b));
            Assert.That(_engine.Map(_a).To(typeof (B)), Is.EqualTo(_b));

            Assert.That(_b.MapTo(typeof (A), _engine), Is.EqualTo(_a));
            Assert.That(_engine.Map(_b).To(typeof (A)), Is.EqualTo(_a));
        }

        [Test]
        public void CanMapEnumerables()
        {
            Assert.That(_aList.MapCollectionTo<B>(_engine), Is.EqualTo(_bList));
            Assert.That(_engine.MapCollection(_aList).To<B>(), Is.EqualTo(_bList));

            Assert.That(_bList.MapCollectionTo<A>(_engine), Is.EqualTo(_aList));
            Assert.That(_engine.MapCollection(_bList).To<A>(), Is.EqualTo(_aList));

            Assert.That(_aList.MapCollectionTo(typeof (B), _engine), Is.EqualTo(_bList));
            Assert.That(_engine.MapCollection(_aList).To(typeof (B)), Is.EqualTo(_bList));

            Assert.That(_bList.MapCollectionTo(typeof (A), _engine), Is.EqualTo(_aList));
            Assert.That(_engine.MapCollection(_bList).To(typeof (A)), Is.EqualTo(_aList));
        }

        [Test]
        public void RoundTrippingWorks()
        {
            Assert.That(_a.MapTo<B>(_engine).MapTo<A>(_engine), Is.EqualTo(_a));
            Assert.That(_engine.Map(_engine.Map(_a).To<B>()).To<A>(), Is.EqualTo(_a));
        }
    }
}