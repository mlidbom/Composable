using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Composable.Messaging.Buses.Implementation
{
    interface IServiceBusPersistenceLayer
    {

        interface IOutboxPersistenceLayer
        {
            void SaveMessage(OutboxMessageWithReceivers messageWithReceivers);
            int MarkAsReceived(Guid messageId, Guid endpointId);
            Task InitAsync();
        }

        interface IInboxPersistenceLayer
        {
            void SaveMessage(Guid messageId, Guid typeId, string serializedMessage);
            void MarkAsSucceeded(Guid messageId);
            int RecordException(Guid messageId, string exceptionStackTrace, string exceptionMessage, string exceptionType);
            int MarkAsFailed(Guid messageId);
            Task InitAsync();
        }

        class OutboxMessageWithReceivers
        {
            public OutboxMessageWithReceivers(string serializedMessage, Guid typeIdGuidValue, Guid messageId, IEnumerable<Guid> receiverEndpointIds)
            {
                SerializedMessage = serializedMessage;
                TypeIdGuidValue = typeIdGuidValue;
                MessageId = messageId;
                ReceiverEndpointIds = receiverEndpointIds.ToList();
            }

            public string SerializedMessage { get; }
            public Guid TypeIdGuidValue { get; }
            public Guid MessageId { get; }
            public IEnumerable<Guid> ReceiverEndpointIds { get; }
        }

        static class InboxMessageDatabaseSchemaStrings
        {
            internal const string TableName = "InboxMessages";

            internal const string GeneratedId = nameof(GeneratedId);
            internal const string TypeId = nameof(TypeId);
            internal const string MessageId = nameof(MessageId);
            internal const string Body = nameof(Body);
            public const string Status = nameof(Status);
            public const string ExceptionCount = nameof(ExceptionCount);
            public const string ExceptionMessage = nameof(ExceptionMessage);
            public const string ExceptionType = nameof(ExceptionType);
            public const string ExceptionStackTrace = nameof(ExceptionStackTrace);
        }

        static class OutboxMessagesDatabaseSchemaStrings
        {
            internal const string TableName = "OutboxMessages";

            internal const string GeneratedId = nameof(GeneratedId);
            internal const string TypeIdGuidValue = nameof(TypeIdGuidValue);
            internal const string MessageId = nameof(MessageId);
            internal const string SerializedMessage = nameof(SerializedMessage);
        }

        static class OutboxMessageDispatchingTableSchemaStrings
        {
            internal const string TableName = "OutboxMessageDispatching";

            internal const string MessageId = nameof(MessageId);
            internal const string EndpointId = nameof(EndpointId);
            internal const string IsReceived = nameof(IsReceived);
        }
    }
}
