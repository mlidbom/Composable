using Event=Composable.Persistence.Common.EventStore.EventTableSchemaStrings;
using Lock = Composable.Persistence.Common.EventStore.AggregateLockTableSchemaStrings;

namespace Composable.Persistence.Common.EventStore
{
    static class EventTableSchemaStrings
    {
        public const string TableName = "Event";

        public const string ReadOrderType = "decimal(38,19)";

        public const string AggregateId = nameof(AggregateId);
        public const string InsertedVersion = nameof(InsertedVersion);
        public const string EffectiveVersion = nameof(EffectiveVersion);
        public const string InsertionOrder = nameof(InsertionOrder);
        public const string ReadOrder = nameof(ReadOrder);

        ///<summary>Used only by persistence layers that cannot store a decimal(38,19). They are forced to use two columns.</summary>
        internal const string ReadOrderIntegerPart = nameof(ReadOrderIntegerPart);
        ///<summary>Used only by persistence layers that cannot store a decimal(38,19). They are forced to use two columns.</summary>
        internal const string ReadOrderFractionPart = nameof(ReadOrderFractionPart);

        public const string TargetEvent = nameof(TargetEvent);
        public const string RefactoringType = nameof(RefactoringType);
        public const string UtcTimeStamp = nameof(UtcTimeStamp);
        public const string SqlInsertTimeStamp = nameof(SqlInsertTimeStamp);
        public const string EventType = nameof(EventType);
        public const string EventId = nameof(EventId);
        public const string Event = nameof(Event);
    }

    static class AggregateLockTableSchemaStrings
    {
        public const string TableName = "AggregateLock";
        public const string AggregateId = Event.AggregateId;
    }
}