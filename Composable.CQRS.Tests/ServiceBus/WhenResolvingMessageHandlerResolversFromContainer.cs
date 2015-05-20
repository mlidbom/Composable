using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.ServiceBus;
using FluentAssertions;
using NSpec;
using NUnit.Framework;
using System;

namespace CQRS.Tests.ServiceBus
{
    [TestFixture]
    public class WhenResolvingMessageHandlerResolversFromContainer : MessageHandlersTestBase
    {
        [SetUp]
        public void RegisterComponents()
        {
            Container.Register(
                Component.For<MessageHandlerResolver>().ImplementedBy<InProcessMessageHandlerResolver>(),
                Component.For<MessageHandlerResolver>().ImplementedBy<ACustomMessageHandlerResolver>(),
                Component.For<IMessageHandlerResolversProvider>().ImplementedBy<WindsorContainerizedMessageHandlerResolversProvider>(),
                Component.For<ACustomMessageHandler, IHandleCustomMessage<AMessage>>().ImplementedBy<ACustomMessageHandler>()
                );
        }

        [Test]
        public void All_registered_message_handler_resolvers_are_resolved()
        {
            Container.Resolve<IMessageHandlerResolversProvider>().GetResolvers().Count.Is(2);
        }

        [Test]
        public void Resolved_message_handler_resolver_invokes_all_message_handler_of_the_specified_type()
        {
            SynchronousBus.Publish(new AMessage());
            Container.Resolve<ACustomMessageHandler>().ReceivedMessage.Should().Be(true);
        }

        public class ACustomMessageHandlerResolver : MessageHandlerResolver
        {
            public ACustomMessageHandlerResolver(IWindsorContainer container) : base(container) { }
            override public Type HandlerInterfaceType { get { return typeof(IHandleCustomMessage<>); } }
        }

        public interface IHandleCustomMessage<T>
        {
            void HandelingMethod(T message);
        }

        public class ACustomMessageHandler : IHandleCustomMessage<AMessage>
        {
            public bool ReceivedMessage { get; private set; }
         
            public void HandelingMethod(AMessage message)
            {
                ReceivedMessage = true;
            }

        }
    }
}
