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

        internal AggregateEventRefactoringInformation? RefactoringInformation { get; set; }
    }

    class AggregateEventRefactoringInformation
    {
        internal static AggregateEventRefactoringInformation Replaces(Guid eventId) => new AggregateEventRefactoringInformation(eventId, EventRefactoringType.Replace);
        internal static AggregateEventRefactoringInformation InsertBefore(Guid eventId) => new AggregateEventRefactoringInformation(eventId, EventRefactoringType.InsertBefore);
        internal static AggregateEventRefactoringInformation InsertAfter(Guid eventId) => new AggregateEventRefactoringInformation(eventId, EventRefactoringType.InsertAfter);

        public AggregateEventRefactoringInformation(Guid targetEvent, EventRefactoringType refactoringType)
        {
            TargetEvent = targetEvent;
            RefactoringType = refactoringType;
        }

        public Guid TargetEvent { get; }
        public EventRefactoringType RefactoringType { get; }
    }

    enum EventRefactoringType
    {
        Replace = 1,
        InsertBefore = 2,
        InsertAfter = 3
    }
}
