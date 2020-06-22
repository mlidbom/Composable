namespace Composable.Messaging.Buses.Implementation
{
    static class InterprocessTransportMessageDispatchingDatabaseSchemaNames
    {
        internal const string TableName = nameof(InterprocessTransportMessageDispatchingDatabaseSchemaNames);

        internal const string MessageId = nameof(MessageId);
        internal const string EndpointId = nameof(EndpointId);
        internal const string IsReceived = nameof(IsReceived);
    }
}
