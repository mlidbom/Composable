﻿using System;

namespace Composable.Persistence.Common.EventStore
{
    static class EventTable
    {
        public static string Name { get; } = "Event";

        public const string ReadOrderType = "decimal(38,19)";

        internal static class Columns
        {
            public const string AggregateId = nameof(AggregateId);
            public const string InsertedVersion = nameof(InsertedVersion);
            public const string EffectiveVersion = nameof(EffectiveVersion);
            public const string InsertionOrder = nameof(InsertionOrder);
            public const string EffectiveOrder = nameof(EffectiveOrder);

            [Obsolete]public const string ReadOrder = nameof(ReadOrder);
            [Obsolete]public const string ReadOrderOrderOffset = nameof(ReadOrderOrderOffset);

            public const string TargetEvent = nameof(TargetEvent);
            public const string RefactoringType = nameof(RefactoringType);
            public const string UtcTimeStamp = nameof(UtcTimeStamp);
            public const string SqlInsertTimeStamp = nameof(SqlInsertTimeStamp);
            public const string EventType = nameof(EventType);
            public const string EventId = nameof(EventId);
            public const string Event = nameof(Event);
        }
    }
}