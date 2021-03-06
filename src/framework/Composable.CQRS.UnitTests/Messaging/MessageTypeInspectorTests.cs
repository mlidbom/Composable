﻿using System;
using Composable.Messaging;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Composable.Tests.Messaging
{
    interface INonGenericWrapperEvent : IWrapperEvent<IEvent>{}
    interface INonCovariantTypeParameterWrapperEvent : IWrapperEvent<IEvent> {}

    [TestFixture]public class MessageTypeInspector_throws_MessageTypeDesignViolationException_if_
    {
        static void AssertInvalidForSending<TMessage>() => Assert.Throws<MessageTypeInspector.MessageTypeDesignViolationException>(MessageInspector.AssertValid<TMessage>);
        static void AssertInvalidForSubscription<TMessage>() => Assert.Throws<MessageTypeInspector.MessageTypeDesignViolationException>(MessageInspector.AssertValidForSubscription<TMessage>);


        [TestFixture]public class Inspecting_type_for_subscription_
        {
            public class Type_implements_Wrapper_event_interface_but_
            {
                [Test] public void Is_not_generic() => AssertInvalidForSubscription<INonGenericWrapperEvent>();

                [Test] public void Does_not_have_a_covariant_type_parameter() => AssertInvalidForSubscription<INonCovariantTypeParameterWrapperEvent>();
            }
        }


        [TestFixture]public class Inspecting_type_for_sending_and_
        {
            public class Type_implements_Wrapper_event_interface_but_
            {
                [Test] public void Is_not_generic() => AssertInvalidForSubscription<INonGenericWrapperEvent>();

                [Test] public void Does_not_have_a_covariant_type_parameter() => AssertInvalidForSubscription<INonCovariantTypeParameterWrapperEvent>();
            }

            interface INotMessage{}
            [Test] public void Is_not_IMessage() => AssertInvalidForSending<INotMessage>();

            interface ICommandAndEvent : IEvent, ICommand{}
            [Test] public void Is_Both_command_and_event() => AssertInvalidForSending<ICommandAndEvent>();

            interface ICommandAndQuery : IEvent, IQuery<object> {}
            [Test] public void Is_Both_command_and_query() => AssertInvalidForSending<ICommandAndQuery>();

            interface IStrictlyLocalAndRemotable : IRemotableMessage, IStrictlyLocalMessage {}
            [Test] public void Is_Both_strictly_local_and_remotable() => AssertInvalidForSending<IStrictlyLocalAndRemotable>();

            interface IForbidAndRequireTransactionalSender : IMustBeSentTransactionally, ICannotBeSentRemotelyFromWithinTransaction {}
            [Test] public void Forbids_and_requires_transactional_sender() => AssertInvalidForSending<IForbidAndRequireTransactionalSender>();

            [UsedImplicitly]class AtMostOnceCommandSettingMessageIdInDefaultConstructor : IAtMostOnceHypermediaCommand
            {
                public Guid MessageId { get; } = Guid.NewGuid();
            }

            [Test] public void Is_at_most_once_command_and_sets_MessageId_in_defaultConstructor() => AssertInvalidForSending<AtMostOnceCommandSettingMessageIdInDefaultConstructor>();


        }
    }
}
