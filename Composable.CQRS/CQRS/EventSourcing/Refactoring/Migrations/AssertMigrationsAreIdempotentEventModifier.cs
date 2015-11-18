using System.Collections.Generic;

namespace Composable.CQRS.EventSourcing.Refactoring.Migrations
{
    internal class AssertMigrationsAreIdempotentEventModifier : IEventModifier
    {
        public static readonly IEventModifier Instance = new AssertMigrationsAreIdempotentEventModifier();
        private AssertMigrationsAreIdempotentEventModifier() { }

        public void Replace(IReadOnlyList<AggregateRootEvent> events) { throw new NonIdempotentMigrationDetectedException(); }

        public void InsertBefore(IReadOnlyList<AggregateRootEvent> insert) { throw new NonIdempotentMigrationDetectedException(); }
    }
}
