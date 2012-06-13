//#region usings

//using AutoMapper;
//using AutoMapper.Mappers;
//using NUnit.Framework;

//#endregion

//namespace Composable.AutoMapper.Tests
//{
//    [TestFixture]
//    public class AfterInitializingWithMappingEngineProvider
//    {
//        private struct A
//        {
//            public string Title { get; set; }
//        }

//        private struct B
//        {
//            public string Title { get; set; }
//        }

//        private A _a = new A { Title = "A" };
//        private B _b = new B { Title = "A" };

//        [TestFixtureSetUp]
//        public void Setup()
//        {
//            var configuration = new ConfigurationStore(new TypeMapFactory(), MapperRegistry.AllMappers());
//            configuration.AssertConfigurationIsValid();
//            configuration.CreateMap<A, B>();
//            configuration.CreateMap<B, A>();

//            var engine = new MappingEngine(configuration);

//            ComposableMapper.Init(() => engine);
//        }

//        [Test]
//        public void ManuallyConfiguredMappingsWork()
//        {
//            Assert.That(_a.MapTo<B>(), Is.EqualTo(_b));
//            Assert.That(_b.MapTo<A>(), Is.EqualTo(_a));

//            Assert.That(_a.MapTo(typeof(B)), Is.EqualTo(_b));
//            Assert.That(_b.MapTo(typeof(A)), Is.EqualTo(_a));
//        }

//        [Test]
//        public void RoundTrippingWorks()
//        {
//            Assert.That(_a.MapTo<B>().MapTo<A>(), Is.EqualTo(_a));
//        }
//    }
//}