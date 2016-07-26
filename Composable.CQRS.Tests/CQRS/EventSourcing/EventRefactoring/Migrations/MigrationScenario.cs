using System;
using System.Collections.Generic;
using System.Linq;
using Composable.CQRS.EventSourcing.Refactoring.Migrations;

namespace CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    public class MigrationScenario
    {
        public readonly IEnumerable<Type> OriginalHistory;
        public readonly IEnumerable<Type> ExpectedHistory;
        public readonly IReadOnlyList<IEventMigration> Migrations;
        public MigrationScenario(IEnumerable<Type> originalHistory, IEnumerable<Type> expectedHistory, params IEventMigration[] migrations)
        {
            OriginalHistory = originalHistory;
            ExpectedHistory = expectedHistory;
            Migrations = migrations.ToList();
        }
    }
}