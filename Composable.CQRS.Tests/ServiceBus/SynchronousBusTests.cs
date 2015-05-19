using System;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.ServiceBus;
using Composable.SystemExtensions.Threading;
using FluentAssertions;
using NServiceBus;
using NUnit.Framework;

namespace CQRS.Tests.ServiceBus
{
    [TestFixture]
    public class SynchronousBusTests
    {
        private IWindsorContainer _container;
        private IDisposable _scope;

        [SetUp]
        public void SetupContainerAndBeginScope()
        {
            _container = new WindsorContainer();
            _container.Register(
                Component.For<IServiceBus>().ImplementedBy<SynchronousBus>().LifestyleScoped(),
                Component.For<IWindsorContainer>().Instance(_container).LifestyleScoped(),
                Component.For<ISingleContextUseGuard>().ImplementedBy<SingleThreadUseGuard>(),
                Component.For<CandidateUpdater, IHandleMessages<INamePropertyUpdatedMessage>, IHandleInProcessMessages<IAgePropertyUpdatedMessage>, IHandleRemoteMessages<IPhonePropertyUpdatedMessage>>()
                    .ImplementedBy<CandidateUpdater>().LifestyleScoped(),
                Component.For<CandidateMessageSpy>().Instance(CandidateMessageSpy.Instance).LifestyleSingleton()
                );

            _scope = _container.BeginScope();
        }

        [TearDown]
        public void TearDown()
        {
            _scope.Dispose();
            _container.Resolve<CandidateMessageSpy>().Reset();
        }

        [Test]
        public void When_publish_a_message_should_invoke_multiple_interface_handler()
        {
            //Arrange
            const string name = "Candidate1";
            const int age = 30;
            const string phone = "phoneNumber";
            var editCandidateMessage = new CandidateEditedMessage(name: name, age: age, phone: phone);

            //Act
            _container.Resolve<IServiceBus>().Publish(editCandidateMessage);

            //Assert
            var candidate = _container.Resolve<CandidateMessageSpy>();
            candidate.Name.Should().Be(name);
            candidate.Age.Should().Be(age);
            candidate.Email.Should().Be(null);
        }

        [Test]
        public void When_send_a_message_should_invoke_multiple_interface_handler()
        {
            //Arrange
            const string name = "Candidate2";
            const int age = 32;
            const string phone = "phoneNumber";
            var editCandidateMessage = new CandidateEditedMessage(name: name, age: age, phone: phone);

            //Act
            Action send= () => _container.Resolve<IServiceBus>().Send(editCandidateMessage);

            //Assert
            send.ShouldThrow<MultipleMessageHandlersRegisteredException>("multiple handlers registered for message");
        }

      

        internal interface INamePropertyUpdatedMessage:IMessage
        {
            string Name { get; }
        }

        internal interface IAgePropertyUpdatedMessage : IMessage
        {
            int Age { get; }
        }

        internal interface IPhonePropertyUpdatedMessage:IMessage
        {
            string Phone { get; }
        }

        internal interface ICandidateEditedMessage
            :INamePropertyUpdatedMessage,
            IAgePropertyUpdatedMessage,
            IPhonePropertyUpdatedMessage
            
        {

        }

        internal class CandidateEditedMessage : ICandidateEditedMessage
        {
            public CandidateEditedMessage(string name,int age,string phone)
            {
                Name = name;
                Age = age;
                Phone = phone;
            }
            public string Name { get; private set; }
            public int Age { get; private set; }
            public string Phone { get; private set; }
        }


        internal class CandidateUpdater : 
            IHandleMessages<INamePropertyUpdatedMessage>,
            IHandleInProcessMessages<IAgePropertyUpdatedMessage>, 
            IHandleRemoteMessages<IPhonePropertyUpdatedMessage>
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

            public void Handle(IPhonePropertyUpdatedMessage message)
            {
                _candidateMessage.Phone = message.Phone;
            }
            
        }

        internal class CandidateMessageSpy
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }

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

            public void Reset()
            {
                _messageSpy = new CandidateMessageSpy();
            }
        }
    }
}
