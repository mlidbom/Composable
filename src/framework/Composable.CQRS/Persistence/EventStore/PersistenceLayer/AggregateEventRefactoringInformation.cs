using System;

namespace Composable.Persistence.EventStore.PersistenceLayer
{
    class AggregateEventRefactoringInformation
    {
        internal static AggregateEventRefactoringInformation Replaces(Guid eventId) => new AggregateEventRefactoringInformation(eventId, AggregateEventRefactoringType.Replace);
        internal static AggregateEventRefactoringInformation InsertBefore(Guid eventId) => new AggregateEventRefactoringInformation(eventId, AggregateEventRefactoringType.InsertBefore);
        internal static AggregateEventRefactoringInformation InsertAfter(Guid eventId) => new AggregateEventRefactoringInformation(eventId, AggregateEventRefactoringType.InsertAfter);

        public AggregateEventRefactoringInformation(Guid targetEvent, AggregateEventRefactoringType refactoringType)
        {
            TargetEvent = targetEvent;
            RefactoringType = refactoringType;
        }

        public Guid TargetEvent { get; }
        public AggregateEventRefactoringType RefactoringType { get; }
    }
}
