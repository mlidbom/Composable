﻿using Castle.MicroKernel.Registration;
using Composable.ServiceBus;
using FluentAssertions;
using NServiceBus;
using NUnit.Framework;

namespace CQRS.Tests.ServiceBus
{
    [TestFixture]
    public class WhenHandlingAMessageThatInheritsOtherMessages : MessageHandlersTestBase
    {
        [SetUp]
        public void RegisterHandlers()
        {
            Container.Register(
                Component.For<CandidateUpdater, IHandleMessages<INamePropertyUpdatedMessage>, IHandleInProcessMessages<IAgePropertyUpdatedMessage>>()
                    .ImplementedBy<CandidateUpdater>()
                    .LifestyleScoped()
                );
        }

        [Test]
        public void When_publishing_a_message_all_matching_handlers_should_be_invoked()
        {
            SynchronousBus.Publish(new CandidateEditedMessage());

            var candidateUpdater = Container.Resolve<CandidateUpdater>();
            candidateUpdater.NamePropertyUpdatedEventReceived.Should().Be(true);
            candidateUpdater.AgePropertyUpdatedEventReceived.Should().Be(true);
        }

        [Test]
        public void Sending_a_message_throws_MultipleHandlersRegisteredException()
        {
            SynchronousBus.Invoking(bus => bus.Send(new CandidateEditedMessage()))
                .ShouldThrow<MultipleMessageHandlersRegisteredException>("multiple handlers registered for message");
        }

        public interface INamePropertyUpdatedMessage : IMessage {}

        public interface IAgePropertyUpdatedMessage : IMessage { }

        public interface ICandidateEditedMessage :
            INamePropertyUpdatedMessage,
            IAgePropertyUpdatedMessage {}

        public class CandidateEditedMessage : ICandidateEditedMessage { }


        public class CandidateUpdater :
            IHandleMessages<INamePropertyUpdatedMessage>,
            IHandleInProcessMessages<IAgePropertyUpdatedMessage>
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
