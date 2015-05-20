using System;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
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
            Container.Register(
                Component.For<ISingleContextUseGuard>().ImplementedBy<SingleThreadUseGuard>(),
                Component.For<SynchronousBus>(),
                Component.For<IWindsorContainer>().Instance(Container)
                );

            _scope = Container.BeginScope();
        }

        [TearDown]
        public void TearDown()
        {
            _scope.Dispose();
        }

        public class AMessage : IMessage { }
        public class InProcessMessageBase:IMessage
        {
             
        }
        public class InProcessMessage : InProcessMessageBase
        {
             
        }
        
        public class RemoteMessageBase:IMessage
        {
             
        }
        public class RemoteMessage : RemoteMessageBase
        {
             
        }


        public class AMessageHandler : IHandleMessages<AMessage>
        {
            public bool ReceivedMessage;

            public void Handle(AMessage message)
            {
                ReceivedMessage = true;
            }
        }

        public class ASpy : AMessageHandler, ISynchronousBusMessageSpy { }

        public class AInProcessMessageHandler : IHandleInProcessMessages<AMessage>
        {
            public bool ReceivedMessage;

            public void Handle(AMessage message)
            {
                ReceivedMessage = true;
            }
        }

        public class ARemoteMessageHandler : IHandleRemoteMessages<AMessage>
        {
            public bool ReceivedMessage;

            public void Handle(AMessage message)
            {
                ReceivedMessage = true;
            }
        } 

        public class MessageHandler:
            IHandleMessages<AMessage>,
            IHandleInProcessMessages<InProcessMessage>,
            IHandleRemoteMessages<RemoteMessageBase>
        {
            public bool ReceiveAMessage = false;
            public bool ReceiveInProcessMessage = false;
            public bool ReceiveRemoteMessage = false;
            public void Handle(AMessage message)
            {
                ReceiveAMessage = true;
            }

            public void Handle(InProcessMessage message)
            {
                ReceiveInProcessMessage = true;
            }

            public void Handle(RemoteMessage message)
            {
                ReceiveRemoteMessage = true;
            }

            public void Handle(RemoteMessageBase message)
            {
                ReceiveRemoteMessage = true;
            }
        }
    }
}
