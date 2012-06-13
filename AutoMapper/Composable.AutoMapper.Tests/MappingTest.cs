//#region usings

//using Castle.MicroKernel.Registration;
//using Castle.Windsor;
//using CommonServiceLocator.WindsorAdapter;
//using NUnit.Framework;

//#endregion

//namespace Composable.AutoMapper.Tests
//{
//    public class MappingTest
//    {
//        [SetUp]
//        public void Setup()
//        {
//            var container = new WindsorContainer();
//            var locator = new WindsorServiceLocator(container);
//            container.Register(AllTypes.FromThisAssembly().BasedOn<IProvidesMappings>().WithService.Base());
//            ComposableMapper.Init(locator);
//        }

//        [TearDown]
//        public void TearDown()
//        {
//            ComposableMapper.ResetOnlyCallFromTests();
//        }
//    }
//}