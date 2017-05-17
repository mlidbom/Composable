// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Local

using System;
// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Composable.Messaging.Buses.APIDraft
{
    // ReSharper disable once UnusedMember.Global
    public class PolicyBasedv2
    {
        void IllustratateRegistration()
        {
            var defaultEventHandlerPolicies = new CompositePolicy(
                Policy.LockExclusively.ThisHandler,//Ensures that this handler is never invoked in parallel with itself.
                Policy.LockExclusively.CurrentMessage//Ensures that no other handler handle the same message in parallel with this handler. Useless when applied to a command handler since there can only be one.
            );

            var defaultCommandHandlerPolicies = new CompositePolicy(
                Policy.LockExclusively.AggregateRelatedToMessage
                );

            var policiesAsInterfaces = new Endpoint(
                //Command handlers
                CommandHandler.For<CreateAccountCommand>("17893552-D533-4A59-A177-63EAF3B7B07E",
                                                         command => {},
                                                         defaultCommandHandlerPolicies,
                                                         Policy.Updates<AccountAggregate>.WithCurrentMessageAggregateId(),//No message handler is allowed to handle a message related to this aggregate in parallel with this handler.
                                                         Policy.RequiresUpToDate<EmailToAccountLookupModel>.All),//This handler must wait until there are no messages queued to any handler with policy: Policy.Updates<EmailToAccountLookupModel>

                //Event handlers
                EventHandler.For<AccountCreatedEvent>("2E8642CA-6C60-4B91-A92E-54AD3753E7F2",
                                                      @event => {},
                                                      defaultEventHandlerPolicies,
                                                      Policy.Updates<AccountReadModel>.WithCurrentMessageAggregateId()),

                EventHandler.For<AccountCreatedEvent>("E59B41A3-BF32-4B7A-B497-F29E3AF42D42",
                                                      @event => {},
                                                      defaultEventHandlerPolicies,
                                                      Policy.OnCascadedMessage.InvokeWithinTriggeringTransaction, //(Deprecated. See: Policy.RequiresUpToDate above. )This denormalizer keeps a domain read model up to date. For the domain to work reliably it needs to be executed within the triggering transaction.
                                                      Policy.Updates<EmailToAccountLookupModel>.WithId(new ExtractEmailFromEmailUpdatedEvent())),

                //How to delegate to container registered component to handle the event.
                EventHandler.For("6E0EA0E6-67DB-4D25-AFE5-99E67130773D", (AccountCreatedEvent @event, AccountController controller) => controller.Handle(@event)),


                //Illustrate that the injection above is just a special case of the generic parameter injection we for registered handlers.
                EventHandler.For("85966417-20B9-4373-9A4B-8398ECA86429", (AccountCreatedEvent @event, AccountController controller, ISomeDependency someDependency) => {})
            );
        }

        #region secret sauce


        interface IMessageHandlerPolicy { }
        interface IThreadingPolicy : IMessageHandlerPolicy { } //IEnumerable<string> LocksToTake(IMessage message);

        interface ITransactionPolicy : IMessageHandlerPolicy { } // String TransactionToParticipateIn(IMessage message)

        static class Policy
        {
            public static class LockExclusively
            {
                public static IMessageHandlerPolicy ThisHandler;
                public static IMessageHandlerPolicy CurrentMessage;
                public static IMessageHandlerPolicy AggregateRelatedToMessage;
            }

            public static class Updates<T>
            {
                public static IMessageHandlerPolicy WithCurrentMessageAggregateId() => null;
                public static IMessageHandlerPolicy WithId(IMessageDataExtractor extractEmailFromEmailUpdatedEvent) => null;
            }

            public static class RequiresUpToDate<T>
            {
                public static IMessageHandlerPolicy All => null;
                public static IMessageHandlerPolicy WithCurrentMessageAggregateId => null;
            }

            public static class OnCascadedMessage
            {
                public static IMessageHandlerPolicy InvokeWithinTriggeringTransaction;
            }
        }

        interface ISomeDependency { }
        interface IMessageDataExtractor { }
        class ExtractEmailFromEmailUpdatedEvent : IMessageDataExtractor { }

        interface IMessageHandler{ }

        class MessageHandler
        {
            public static IMessageHandler For<T>(string uniqueId, Action<T> handler, params IMessageHandlerPolicy[] policies) => null;
            public static IMessageHandler For<T, D1>(string uniqueId, Action<T, D1> handler, params IMessageHandlerPolicy[] policies) => null;
            public static IMessageHandler For<T, D1, D2>(string uniqueId, Action<T, D1, D2> handler, params IMessageHandlerPolicy[] policies) => null;
        }
        class EventHandler : MessageHandler
        {
        }

        class CommandHandler : MessageHandler
        {
        }

        class Endpoint
        {
            public Endpoint(params IMessageHandler[] messageHandlers) { }
        }

        class CompositePolicy : IMessageHandlerPolicy
        {
            public CompositePolicy(params IMessageHandlerPolicy[] policies) { }
        }

        #endregion

    }
}
