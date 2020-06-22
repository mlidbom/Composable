namespace Composable.Messaging.Buses.Implementation
{
    static class InterprocessTransportOutboxMessagesDatabaseSchemaStrings
    {
        internal const string TableName = nameof(InterprocessTransportOutboxMessagesDatabaseSchemaStrings);

        internal const string Identity = nameof(Identity);
        internal const string TypeIdGuidValue = nameof(TypeIdGuidValue);
        internal const string MessageId = nameof(MessageId);
        internal const string Body = nameof(Body);
    }
}
