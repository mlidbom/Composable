using Composable.DependencyInjection;
using Composable.Testing;
using Composable.Testing.Performance;
using FluentAssertions.Extensions;
using NCrunch.Framework;
using NUnit.Framework;

namespace Composable.Tests.KeyValueStorage
{
    [Performance, LongRunning, Serial]
    [TestFixture] class DocumentDbPerformanceTests : DocumentDbTestsBase
    {
        [Test] public void Saves_100_documents_in_milliseconds_msSql_75_MySql_300_InMemory_8_PgSql_75_Orcl_100()
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

                //Urgent: Fix the MySql opening connection slowness problem and up the number for MySql in this test
                //Urgent: See if using stored procedures and/or prepared statements speeds this up.
                TimeAsserter.Execute(
                    action: SaveOneNewUserInTransaction,
                    iterations: 100,
                    maxTotal: TestEnvironment.ValueForPersistenceProvider(inMem:8, msSql:75, pgSql: 75, orcl: 100, mySql:300).Milliseconds()
                );
            });
        }
    }
}
