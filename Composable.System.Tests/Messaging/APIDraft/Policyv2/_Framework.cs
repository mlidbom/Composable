// ReSharper disable All

using System;
using Composable.Messaging;

namespace Composable.Tests.Messaging.APIDraft.Policyv2
{

    interface IHandlerPolicyConfigurationBuilder
    {
        void ExclusivelyLock(string resource);
        void InclusivelyLock(string resource);
        void Updates(Type updatedType);
        void Updates(Type updatedType, string id);
        void RequiresUpdtodate(Type required);
        void RequiresUpdtodate(Type required, string id);
        void TriggerWithinPublishingTransaction();
    }

    interface IMessageHandlerPolicy
    {
        void Configure(IHandlerPolicyConfigurationBuilder builder, IMessage message);
    }


    static class Policy
    {
        public static IMessageHandlerPolicy NoRestrictions => null;
        public static IMessageHandlerPolicy Publishes<T>() => null;
        public static IMessageHandlerPolicy Sends<T>() => null;

        public static class LockExclusively
        {
            public static IMessageHandlerPolicy ThisHandler;
            public static IMessageHandlerPolicy CurrentMessage;
            public static IMessageHandlerPolicy AggregateRelatedToMessage;
            public static IMessageHandlerPolicy MessageProcessing => null;
            public static IMessageHandlerPolicy CommandProcessing => null;
            public static IMessageHandlerPolicy EventProcessing => null;
        }

        public class Inclusivelock
        {
            public static IMessageHandlerPolicy MessageProcessing => null;
            public static IMessageHandlerPolicy CommandProcessing => null;
            public static IMessageHandlerPolicy EventProcessing => null;
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

    interface IMessageHandler { }

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
        public void Configure(IHandlerPolicyConfigurationBuilder builder, IMessage message) { throw new NotImplementedException(); }
    }
}
