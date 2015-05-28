using System;
using System.Collections.Generic;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.Windsor.Testing;
using Composable.ServiceBus;
using Composable.SystemExtensions.Threading;
using NServiceBus;
using NUnit.Framework;

namespace CQRS.Tests.ServiceBus
{
    [TestFixture]
    public class MessageHandlersTestBase
    {
        protected WindsorContainer Container;
        private IDisposable _scope;

        public SynchronousBus SynchronousBus { get { return Container.Resolve<SynchronousBus>(); } }



        [SetUp]
        public void SetUpContainerAndBeginScope()
        {
            Container = new WindsorContainer();
            Container.ConfigureWiringForTestsCallBeforeAllOtherWiring();
            Container.Register(
                Component.For<SynchronousBus>(),
                Component.For<EventsReplayer>(),
                Component.For<IWindsorContainer>().Instance(Container));
            new MessageHandlersRegister().RegisterMessageHandlersForTestingFromAssemblyContaining<MessageHandlersTestBase>(Container);
            Container.ConfigureWiringForTestsCallAfterAllOtherWiring();
            _scope = Container.BeginScope();
        }

        [TearDown]
        public void TearDown()
        {
            _scope.Dispose();
        }
    }
}
