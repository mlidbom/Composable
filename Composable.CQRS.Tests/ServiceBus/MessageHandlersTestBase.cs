using System;
using System.Globalization;
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


        //Review:mlidbo: I think all these messages and handlers here makes the test code confusing and hard to read because it all uses these abstract events and handlers instead of classes made specifically for that test. 
        //Review:mlidbo:I suspect it is too much focus on DRY and not enough on SOLID and expressive code. 
        //Review:mlidbo: I suspect this code being here instead of in the tests that use it is part of the reason for that test code being strange/wrong. 
        //Review:mlidbo: You have to go here and look carefully at the inheritance hierarchy of these abstract classes to realize that the inheritance hierarchy makes what the tests "test" impossible...
        //Review:mlidbo: If this code was in the tests instead it would be much easier to understand what they test, and to realize that it is wrong/impossible. 
        //Review:mlidbo:I suggest creating specific concrete messages and handlers in the tests that need them instead of sharing abstract messages and handlers from here.
        //Review:mlidbo: Remember: SOLID and expressive is much more important than DRY :)
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

        public class ReplayEventBase:IMessage
        {
            
        }

        public class ReplayEvent:ReplayEventBase
        {
 
        }

        public class HandleAndReplayEventBase:IMessage
        {
 
        }

        public class HandleAndReplayEvent:HandleAndReplayEventBase
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

        public class AHandleReplayedEventsHandler:IHandleReplayedEvents<AMessage>
        {
            public bool ReceivedMessage;
            public void Handle(AMessage @event)
            {
                ReceivedMessage = true;
            }
        }

        public class AHandleReplayedAndPublishedEventsHandler : IHandleReplayedAndPublishedEvents<AMessage>
        {
            public bool ReceivedMessage;
            public void Handle(AMessage @event)
            {
                ReceivedMessage = true;
            }
        }

        public class MessageHandler:
            IHandleMessages<AMessage>,
            IHandleInProcessMessages<InProcessMessage>,
            IHandleRemoteMessages<RemoteMessageBase>,
            IHandleReplayedEvents<ReplayEventBase>,
            IHandleReplayedAndPublishedEvents<HandleAndReplayEventBase>
        {
            public bool ReceivedAMessage = false;
            public bool ReceivedInProcessMessage = false;
            public bool ReceivedRemoteMessage = false;
            public bool ReceivedReplayEvent = false;
            public bool ReceivedHandleAndReplayEvent = false;
            public void Handle(AMessage message)
            {
                ReceivedAMessage = true;
            }

            public void Handle(InProcessMessage message)
            {
                ReceivedInProcessMessage = true;
            }

            public void Handle(RemoteMessage message)
            {
                ReceivedRemoteMessage = true;
            }

            public void Handle(RemoteMessageBase message)
            {
                ReceivedRemoteMessage = true;
            }

            public void Handle(ReplayEventBase @event)
            {
                ReceivedReplayEvent = true;
            }

            public void Handle(HandleAndReplayEventBase message)
            {
                ReceivedHandleAndReplayEvent = true;
            }
          
        }
    }
}
