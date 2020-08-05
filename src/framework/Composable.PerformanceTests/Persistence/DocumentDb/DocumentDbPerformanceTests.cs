using Composable.DependencyInjection;
using Composable.Testing;
using Composable.Testing.Performance;
using FluentAssertions.Extensions;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Composable.Tests.Persistence.DocumentDb
{
    [LongRunning]
    class DocumentDbPerformanceTests : DocumentDbTestsBase
    {
        [Test] public void Saves_100_documents_in_milliseconds_msSql_75_MySql_300_InMemory_8_PgSql_100_Orcl_100_DB2_300()
        {
            ServiceLocator.ExecuteInIsolatedScope(() =>
            {
                var updater = ServiceLocator.DocumentDbUpdater();

                void SaveOneNewUserInTransaction()
                {
                    var user = new User();
                    updater.Save(user);
                }

                //Warm up caches etc
                SaveOneNewUserInTransaction();

                //Performance: Fix the MySql opening connection slowness problem and up the number for MySql in this test
                //Performance: Look at why DB2 is so slow here.
                //Performance: See if using stored procedures and/or prepared statements speeds this up.
                TimeAsserter.Execute(
                    action: SaveOneNewUserInTransaction,
                    iterations: 100,
                    maxTotal: TestEnv.PersistenceLayer.ValueFor(db2: 300, memory: 8, msSql: 75, mySql: 300, orcl: 100, pgSql: 75).Milliseconds()
                );
            });
        }

        public DocumentDbPerformanceTests([NotNull] string _) : base(_) {}
    }
}
