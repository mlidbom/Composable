namespace Composable.Persistence.EventStore.Refactoring.Migrations
{
    class AssertMigrationsAreIdempotentEventModifier : IEventModifier
    {
        public static readonly IEventModifier Instance = new AssertMigrationsAreIdempotentEventModifier();
        AssertMigrationsAreIdempotentEventModifier() { }

        public void Replace(params DomainEvent[] events) { throw new NonIdempotentMigrationDetectedException(); }

        public void InsertBefore(params DomainEvent[] insert) { throw new NonIdempotentMigrationDetectedException(); }
    }
}
