namespace Composable.Persistence.EventStore.Refactoring.Migrations
{
    class AssertMigrationsAreIdempotentEventModifier : IEventModifier
    {
        public static readonly IEventModifier Instance = new AssertMigrationsAreIdempotentEventModifier();
        AssertMigrationsAreIdempotentEventModifier() { }

        public void Replace(params AggregateEvent[] events) { throw new NonIdempotentMigrationDetectedException(); }

        public void InsertBefore(params AggregateEvent[] insert) { throw new NonIdempotentMigrationDetectedException(); }
    }
}
