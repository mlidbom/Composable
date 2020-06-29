namespace Composable.Messaging.Buses.Implementation
{
    interface IServiceBusPersistenceLayer
    {

        static class InboxMessageDatabaseSchemaStrings
        {
            internal const string TableName = nameof(InboxMessageDatabaseSchemaStrings);

            internal const string Identity = nameof(Identity);
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
            internal const string TableName = nameof(OutboxMessagesDatabaseSchemaStrings);

            internal const string Identity = nameof(Identity);
            internal const string TypeIdGuidValue = nameof(TypeIdGuidValue);
            internal const string MessageId = nameof(MessageId);
            internal const string Body = nameof(Body);
        }

        static class OutboxMessageDispatchingTableSchemaStrings
        {
            internal const string TableName = nameof(OutboxMessageDispatchingTableSchemaStrings);

            internal const string MessageId = nameof(MessageId);
            internal const string EndpointId = nameof(EndpointId);
            internal const string IsReceived = nameof(IsReceived);
        }
    }
}
