namespace Composable.Persistence.EventStore.PersistenceLayer
{
    class AggregateEventStorageInformation
    {
        internal int InsertedVersion { get; set; }
        internal int EffectiveVersion { get; set; }

        internal ReadOrder? ReadOrder { get; set; }

        internal AggregateEventRefactoringInformation? RefactoringInformation { get; set; }
    }
}
