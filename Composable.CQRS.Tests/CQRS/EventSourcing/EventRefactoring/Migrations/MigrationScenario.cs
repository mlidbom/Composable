using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Composable.CQRS.EventSourcing.Refactoring.Migrations;

namespace CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    public class MigrationScenario
    {
        public readonly IEnumerable<Type> OriginalHistory;
        public readonly IEnumerable<Type> ExpectedHistory;
        public readonly IReadOnlyList<IEventMigration> Migrations;
        public Guid AggregateId { get; }
        private static int Instances = 1;

        public MigrationScenario(IEnumerable<Type> originalHistory, IEnumerable<Type> expectedHistory, params IEventMigration[] migrations)
            : this(Guid.Parse($"00000000-0000-0000-0000-0000000{Instances:D5}"),
                   originalHistory,
                   expectedHistory,
                   migrations) {}

        public MigrationScenario
            (Guid aggregateId,
             IEnumerable<Type> originalHistory,
             IEnumerable<Type> expectedHistory,
             params IEventMigration[] migrations)
        {            
            AggregateId = aggregateId;
            OriginalHistory = originalHistory;
            ExpectedHistory = expectedHistory;
            Migrations = migrations.ToList();
            Interlocked.Increment(ref Instances);
        }
    }
}
