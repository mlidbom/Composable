using System;

namespace Composable.Persistence.EventStore.PersistenceLayer
{
    //Urgent: Refactor into enum Replace,InsertBefore,InsertAfter + RefactoredEventId
    //Urgent: make all properties non-nullable instead make the whole instance on the event nullable
    //Urgent:  move data that is on all events elsewhere
    //Urgent: split that between read and write so that ReadOrder is not nullable when reading and not present when writing.
    class AggregateEventStorageInformation
    {
        internal int InsertedVersion { get; set; }
        internal int EffectiveVersion { get; set; }

        internal IEventStorePersistenceLayer.ReadOrder? ReadOrder { get; set; }
        internal Guid? Replaces { get; set; }
        internal Guid? InsertBefore { get; set; }
        internal Guid? InsertAfter { get; set; }
    }
}
