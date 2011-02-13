using System;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using CommonServiceLocator.WindsorAdapter;
using NUnit.Framework;

namespace Composable.AutoMapper.Tests
{
    [TestFixture]
    public class WhenMoreThanOneDefinitionOfAMappingExists
    {
        [SetUp]
        public void Setup()
        {
            var container = new WindsorContainer();
            var locator = new WindsorServiceLocator(container);
            container.Register(AllTypes.FromThisAssembly().BasedOn<IProvidesMappings>().WithService.Base());
            ComposableMapper.Init(locator);
        }

        [TearDown]
        public void TearDown()
        {
            ComposableMapper.ResetOnlyCallFromTests();
        }

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