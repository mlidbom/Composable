using Castle.MicroKernel.Registration;
using Composable.ServiceBus;
using FluentAssertions;
using JetBrains.Annotations;
using NServiceBus;
using NUnit.Framework;

namespace CQRS.Tests.ServiceBus
{
    [TestFixture]
    public class WhenHandlingAMessageThatInheritsOtherMessages : MessageHandlersTestBase
    {
        [Test]
        public void When_publishing_a_message_all_matching_handlers_should_be_invoked()
        {
            //Act
            SynchronousBus.Publish(new CandidateEditedMessage());

            //Assert
            var candidateUpdater = Container.Resolve<CandidateUpdater>();
            candidateUpdater.NamePropertyUpdatedEventReceived.Should().Be(true);
            candidateUpdater.AgePropertyUpdatedEventReceived.Should().Be(true);
        }

        [Test]
        public void Sending_a_message_throws_MultipleHandlersRegisteredException()
        {
            //Assert
            SynchronousBus.Invoking(bus => bus.Send(new CandidateEditedMessage()))
                .ShouldThrow<MultipleMessageHandlersRegisteredException>("multiple handlers registered for message");
        }

        public interface INamePropertyUpdatedMessage : IMessage {}

        public interface IAgePropertyUpdatedMessage : IMessage { }

        public interface ICandidateEditedMessage :
            INamePropertyUpdatedMessage,
            IAgePropertyUpdatedMessage {}

        public class CandidateEditedMessage : ICandidateEditedMessage { }


        [UsedImplicitly]
        public class CandidateUpdater :
            IHandleMessages<INamePropertyUpdatedMessage>,
            IHandleMessages<IAgePropertyUpdatedMessage>
        {
            public bool NamePropertyUpdatedEventReceived;
            public bool AgePropertyUpdatedEventReceived;

            public void Handle(INamePropertyUpdatedMessage message)
            {
                NamePropertyUpdatedEventReceived = true;
            }

            public void Handle(IAgePropertyUpdatedMessage message)
            {
                AgePropertyUpdatedEventReceived = true;
            }
        }
    }
}
