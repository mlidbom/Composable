using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Registration.Interceptor;
using Castle.Windsor;
using Composable.ServiceBus;
using Composable.SystemExtensions.Threading;
using FluentAssertions;
using NServiceBus;
using NSpec;
using NUnit.Framework;

namespace CQRS.Tests.ServiceBus
{
    [TestFixture]
    public class WhenHandleBasedMessageTests
    {
        private IWindsorContainer _container;
        private IDisposable _scope;
        
        [SetUp]
        public void SetupConatinerAndBeginScope()
        {
            _container=new WindsorContainer();
            _container.Register(
                Component.For<IServiceBus>().ImplementedBy<SynchronousBus>().LifestyleScoped(),
                Component.For<IWindsorContainer>().Instance(_container).LifestyleScoped(),
                Component.For<ISynchronousBusSubscriberFilter>().Instance(PublishToAllSubscribersSubscriberFilter.Instance).LifestyleScoped(),
                Component.For<ISingleContextUseGuard>().ImplementedBy<SingleThreadUseGuard>(),
                Component.For<CandidateUpdater, IHandleMessages<INamePropertyUpdatedMessage>,IHandleMessages<IAgePropertyUpdatedMessage>>()
                .ImplementedBy<CandidateUpdater>().LifestyleScoped(),
                Component.For<CandidateMessageSpy>().Instance(CandidateMessageSpy.Instance).LifestyleSingleton()
                );

            _scope = _container.BeginScope();
        }

        [TearDown]
        public void TearDown()
        {
            _scope.Dispose();
        }

        [Test]
        public void When_publish_a_message_should_invoke_multiple_interface_handler()
        {
            //Arrange
            var name = "Candidate1";
            var age = 30;
            var editCandidateMessage = new CandidateEditedMessage(name: name, age: age);

            //Act
            _container.Resolve<IServiceBus>().Publish(editCandidateMessage);

            //Assert
            var candidate = _container.Resolve<CandidateMessageSpy>();
            candidate.Name.Should().Be(name);
            candidate.Age.Should().Be(age);
        }

        [Test]
        public void When_send_a_message_should_invoke_multiple_interface_handler()
        {
            //Arrange
            var name = "Candidate2";
            var age = 32;
            var editCandidateMessage = new CandidateEditedMessage(name: name, age: age);

            //Act
            _container.Resolve<IServiceBus>().Send(editCandidateMessage);

            //Assert
            var candidate = _container.Resolve<CandidateMessageSpy>();
            candidate.Name.Should().Be(name);
            candidate.Age.Should().Be(age);
        }

        public interface INamePropertyUpdatedMessage:IMessage
        {
            string Name { get; }
        }

        public interface IAgePropertyUpdatedMessage:IMessage
        {
            int Age { get; }
        }

        public interface ICandidateEditedMessage
            :INamePropertyUpdatedMessage,
            IAgePropertyUpdatedMessage
            
        {

        }

        public class CandidateEditedMessage:ICandidateEditedMessage
        {
            public CandidateEditedMessage(string name,int age )
            {
                Name = name;
                Age = age;
            }
            public string Name { get; private set; }
            public int Age { get; private set; }
        }

        public class CandidateUpdater : IHandleMessages<INamePropertyUpdatedMessage>,IHandleMessages<IAgePropertyUpdatedMessage>
        {
            private readonly CandidateMessageSpy _candidateMessage;

            public CandidateUpdater(CandidateMessageSpy candidateMessage)
            {
                _candidateMessage = candidateMessage;
            }

            public void Handle(INamePropertyUpdatedMessage message)
            {
                _candidateMessage.Name = message.Name;
            }

            public void Handle(IAgePropertyUpdatedMessage message)
            {
                _candidateMessage.Age = message.Age;
            }
        }

        public class CandidateMessageSpy
        {
            public string Name { get; set; }
            public int Age { get; set; }

            private static CandidateMessageSpy _messageSpy;

            private CandidateMessageSpy()
            {
                
            }
            public static CandidateMessageSpy Instance
            {
                get
                {
                    if(_messageSpy==null) _messageSpy=new CandidateMessageSpy();
                    return _messageSpy;
                }
            }
        }
    }
}
