namespace Composable.Messaging.Buses.Implementation
{
    static class OutboxMessagesDatabaseSchemaStrings
    {
        internal const string TableName = nameof(OutboxMessagesDatabaseSchemaStrings);

        internal const string Identity = nameof(Identity);
        internal const string TypeIdGuidValue = nameof(TypeIdGuidValue);
        internal const string MessageId = nameof(MessageId);
        internal const string Body = nameof(Body);
    }
}
