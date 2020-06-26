namespace Composable.Messaging.Buses.Implementation
{
    static class OutboxMessageDispatchingTableSchemaStrings
    {
        internal const string TableName = nameof(OutboxMessageDispatchingTableSchemaStrings);

        internal const string MessageId = nameof(MessageId);
        internal const string EndpointId = nameof(EndpointId);
        internal const string IsReceived = nameof(IsReceived);
    }
}
