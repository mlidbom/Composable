namespace Composable.Messaging.Buses.Implementation
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
}
